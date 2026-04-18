using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer1_Framing.Encoding.Abstractions;
using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Logging;
using System.Buffers;
using System.Diagnostics;

namespace MWB.Networking.Layer2_Protocol.Driver;

/// <summary>
/// Drives a ProtocolSession using a NetworkAdapter.
///
/// Owns execution, concurrency, cancellation, and lifetime.
/// Contains no protocol semantics and no transport logic beyond
/// driving the byte->frame decoding pipeline.
/// </summary>
public sealed class ProtocolDriver : IHasLogger
{
    public ProtocolDriver(
        ILogger logger,
        IProtocolSessionRuntime sessionRuntime,
        ProtocolDriverOptions options)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.SessionRuntime = sessionRuntime ?? throw new ArgumentNullException(nameof(sessionRuntime));
        // copy options alocally
        ArgumentNullException.ThrowIfNull(options);
        this.Connection = options.Connection
            ?? throw new ArgumentException(
                $"{nameof(options)}.{nameof(options.Connection)} is required and cannot be null.",
                nameof(options));
        this.Decoder = options.Decoder
            ?? throw new ArgumentException(
                $"{nameof(options)}.{nameof(options.Decoder)} is required and cannot be null.",
                nameof(options));
        this.FrameReader = options.FrameReader
            ?? throw new ArgumentException(
                $"{nameof(options)}.{nameof(options.FrameReader)} is required and cannot be null.",
                nameof(options));
        this.Adapter = options.Adapter
            ?? throw new ArgumentException(
                $"{nameof(options)}.{nameof(options.Adapter)} is required and cannot be null.",
                nameof(options));
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

    private SemaphoreSlim SessionGate
    {
        get;
    } = new(1, 1);

    private TaskCompletionSource WhenReadySource
    {
        get;
    } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task WhenReady
        => this.WhenReadySource.Task;

    /// <summary>
    /// Runs the protocol driver until cancelled or a fatal error occurs.
    /// </summary>
    //[LogMethod]
    public async Task RunAsync(CancellationToken ct)
    {
        using var scope = this.Logger.BeginMethodLoggingScope(this);

        var readTask = this.RunReadLoopAsync(ct);
        var writeTask = this.RunWriteLoopAsync(ct);
        var consumeTask = this.ConsumeFramesAsync(ct);

        this.WhenReadySource.TrySetResult();

        await Task
            .WhenAny(
                readTask,
                writeTask,
                consumeTask)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Drives the transport read loop and feeds bytes into the frame decoder.
    /// Decoded frames are delivered asynchronously via NetworkFrameReader.
    /// </summary>
    //[LogMethod]
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

    /// <summary>
    /// Continuously drains outbound frames from the protocol session
    /// and writes them to the adapter.
    /// </summary>
    //[LogMethod]
    private async Task RunWriteLoopAsync(CancellationToken ct)
    {
        using var scope = this.Logger.BeginMethodLoggingScope(this);

        while (!ct.IsCancellationRequested)
        {
            await this.SessionRuntime
                .WaitForOutboundFrameAsync(ct)
                .ConfigureAwait(false);

            ProtocolFrame? protocolFrame;

            await this.SessionGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (!this.SessionRuntime.TryDequeueOutboundFrame(out protocolFrame))
                {
                    protocolFrame = null;
                }
            }
            finally
            {
                this.SessionGate.Release();
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

    /// <summary>
    /// Consumes decoded NetworkFrames from the adapter and feeds them
    /// into the protocol session.
    /// </summary>
    //[LogMethod]
    public async Task ConsumeFramesAsync(CancellationToken ct)
    {
        using var scope = this.Logger.BeginMethodLoggingScope(this);

        while (!ct.IsCancellationRequested)
        {
            var networkFrame =
                await this.Adapter.ReadFrameAsync(ct).ConfigureAwait(false);

            var protocolFrame = FrameConverter.ToProtocolFrame(networkFrame);

#if ENABLE_PROTOCOL_FRAME_DIAGNOSTICS
            protocolFrame.Diagnostics.ReceivedTimestamp = Stopwatch.GetTimestamp();
            this.RecentInboundFrames.Write(protocolFrame);
#endif

            await this.SessionGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                this.SessionRuntime.ProcessFrame(protocolFrame);
            }
            finally
            {
                this.SessionGate.Release();
            }
        }
    }
}
