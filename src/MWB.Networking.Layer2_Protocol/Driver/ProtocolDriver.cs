using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MWB.Networking.Layer0_Transport;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer1_Framing.Encoding.Abstractions;
using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Logging;
using System.Buffers;
using System.Diagnostics;
using System.Text;

namespace MWB.Networking.Layer2_Protocol.Driver;

/// <summary>
/// Drives a protocol session by executing transport I/O loops and
/// coordinating frame decoding and delivery.
///
/// Owns execution, concurrency, scheduling, and cooperative shutdown.
/// Contains no protocol semantics and no transport logic beyond driving
/// the byte-to-frame pipeline.
/// </summary>
public sealed partial class ProtocolDriver
{
    // ------------------------------------------------------------------
    // Construction
    // ------------------------------------------------------------------

    internal ProtocolDriver(
        ILogger logger,
        IProtocolSessionRuntime sessionRuntime,
        INetworkConnection connection,
        IFrameDecoder decoder,
        NetworkFrameReader frameReader,
        NetworkAdapter adapter)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.SessionRuntime = sessionRuntime ?? throw new ArgumentNullException(nameof(sessionRuntime));
        this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        this.Decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
        this.FrameReader = frameReader ?? throw new ArgumentNullException(nameof(frameReader));
        this.Adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
    }

#if ENABLE_PROTOCOL_FRAME_DIAGNOSTICS
    private RingBuffer<ProtocolFrame> RecentInboundFrames
    {
        get;
    } = new(capacity: 100_000);

    private RingBuffer<ProtocolFrame> RecentOutboundFrames
    {
        get;
    } = new(capacity: 100_000);
#endif

    // ------------------------------------------------------------------
    // Dependencies
    // ------------------------------------------------------------------

    public ILogger Logger
    {
        get;
    }

    private INetworkConnection Connection
    {
        get;
    }

    private IFrameDecoder Decoder
    {
        get;
    }

    private NetworkFrameReader FrameReader
    {
        get;
    }

    private NetworkAdapter Adapter
    {
        get;
    }

    private IProtocolSessionRuntime SessionRuntime
    {
        get;
    }

    /// <summary>
    /// Serializes access to the protocol session runtime to enforce
    /// single-threaded semantic execution across concurrent driver loops.
    /// </summary>
    private SemaphoreSlim SessionRuntimeGate
    {
        get;
    } = new(1, 1);

    // ------------------------------------------------------------------
    // Lifecycle
    // ------------------------------------------------------------------

    private readonly DriverLifecycle _lifecycle = new();

    private readonly TaskCompletionSource _whenStartedSource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Completes when the driver execution loops have been scheduled.
    /// </summary>
    public Task WhenStarted => _whenStartedSource.Task;

    /// <summary>
    /// Starts executing the protocol driver. May be called at most once.
    /// </summary>
    public Task RunAsync(CancellationToken ct)
    {
        using var scope = this.Logger.BeginMethodLoggingScope(this);

        return _lifecycle.Start(token =>
        {
            var linkedCts =
                CancellationTokenSource.CreateLinkedTokenSource(ct, token);

            return this.RunInternalAsync(linkedCts.Token);
        });
    }

    /// <summary>
    /// Requests cooperative shutdown of the driver and waits for execution
    /// to complete. Safe to call multiple times.
    /// </summary>
    public Task StopAsync()
    {
        return _lifecycle.StopAsync();
    }

    private void SignalStarted()
    {
        _whenStartedSource.TrySetResult();
    }

    // ------------------------------------------------------------------
    // Execution
    // ------------------------------------------------------------------

    private async Task RunInternalAsync(CancellationToken ct)
    {
        using var scope = this.Logger.BeginMethodLoggingScope(this);

        var readTask = this.RunReadLoopAsync(ct);
        var writeTask = this.RunWriteLoopAsync(ct);
        var consumeTask = this.ConsumeFramesAsync(ct);

        SignalStarted();

        await Task
            .WhenAny(
                readTask,
                writeTask,
                consumeTask)
            .ConfigureAwait(false);
    }

    // ------------------------------------------------------------------
    // Transport read loop
    // ------------------------------------------------------------------

    /// <summary>
    /// Drives the transport read loop and feeds bytes into the frame decoder.
    /// Decoded frames are delivered asynchronously via NetworkFrameReader.
    /// </summary>
    private async Task RunReadLoopAsync(CancellationToken ct)
    {
        using var scope = this.Logger.BeginMethodLoggingScope(this);
        this.Logger.LogDebug("[DRIVER READ LOOP] entering");

        var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                int bytesRead;
                try
                {
                    bytesRead = await Connection
                        .ReadAsync(buffer, ct)
                        .ConfigureAwait(false);
                    this.Logger.LogDebug(
                        "[DRIVER READ LOOP] ReadAsync returned {BytesRead}",
                        bytesRead);
                }
                catch (OperationCanceledException)
                {
                    this.Logger.LogDebug("[DRIVER READ LOOP] cancelled");
                    return;
                }

                if (bytesRead == 0)
                {
                    this.Logger.LogDebug("[DRIVER READ LOOP] EOF");
                    await this.Decoder
                        .CompleteAsync(this.FrameReader, ct)
                        .ConfigureAwait(false);
                    return;
                }

                var sequence = new ReadOnlySequence<byte>(
                    buffer.AsMemory(0, bytesRead));

                this.Logger.LogDebug("[DRIVER READ LOOP] calling DecodeFrameAsync");
                await this.Decoder
                    .DecodeFrameAsync(sequence, this.FrameReader, ct)
                    .ConfigureAwait(false);
                this.Logger.LogDebug("[DRIVER READ LOOP] returned from DecodeFrameAsync");
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            this.Logger.LogDebug("[DRIVER READ LOOP] leaving");
        }
    }

    // ------------------------------------------------------------------
    // Outbound write loop
    // ------------------------------------------------------------------

    /// <summary>
    /// Continuously drains outbound frames from the protocol session
    /// and writes them to the adapter.
    /// </summary>
    private async Task RunWriteLoopAsync(CancellationToken ct)
    {
        using var scope = this.Logger.BeginMethodLoggingScope(this);

        while (!ct.IsCancellationRequested)
        {
            await this.SessionRuntime
                .WaitForOutboundFrameAsync(ct)
                .ConfigureAwait(false);

            ProtocolFrame? protocolFrame;

            await this.SessionRuntimeGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (!this.SessionRuntime.TryDequeueOutboundFrame(out protocolFrame))
                {
                    protocolFrame = null;
                }
            }
            finally
            {
                this.SessionRuntimeGate.Release();
            }

            if (protocolFrame is null)
            {
                await Task.Yield();
                continue;
            }

#if ENABLE_PROTOCOL_FRAME_DIAGNOSTICS
            RecentOutboundFrames.Write(protocolFrame);
#endif

            var networkFrame = FrameConverter.ToNetworkFrame(protocolFrame);

            try
            {
                await this.Adapter
                    .WriteFrameAsync(networkFrame, ct)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    // ------------------------------------------------------------------
    // Frame consumption loop
    // ------------------------------------------------------------------

    /// <summary>
    /// Consumes decoded NetworkFrames from the adapter and feeds them
    /// into the protocol session.
    /// </summary>
    private async Task ConsumeFramesAsync(CancellationToken ct)
    {
        using var scope = this.Logger.BeginMethodLoggingScope(this);

        while (!ct.IsCancellationRequested)
        {
            var networkFrame = await this.Adapter.ReadFrameAsync(ct)
                .ConfigureAwait(false);

            var protocolFrame = FrameConverter.ToProtocolFrame(networkFrame);

#if ENABLE_PROTOCOL_FRAME_DIAGNOSTICS
            protocolFrame.Diagnostics.ReceivedTimestamp = Stopwatch.GetTimestamp();
            this.RecentInboundFrames.Write(protocolFrame);
#endif

            await this.SessionRuntimeGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                this.SessionRuntime.ProcessFrame(protocolFrame);
            }
            finally
            {
                this.SessionRuntimeGate.Release();
            }
        }
    }
}
