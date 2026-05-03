using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Pipeline;
using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Logging;
using System.Buffers;
using System.Diagnostics;

namespace MWB.Networking.Layer2_Protocol.Driver;

/// <summary>
/// Drivers a protocol session over a transport by running
/// read / write / consume loops.
///
/// Layer 2.5:
/// - Knows protocol internals
/// - Owns execution, concurrency, and shutdown
/// - Does NOT own lifecycle policy
/// - Does NOT define protocol semantics
/// </summary>
public sealed partial class ProtocolDriver
{
    // ------------------------------------------------------------------
    // Construction
    // ------------------------------------------------------------------

    internal ProtocolDriver(
        ILogger logger,
        IProtocolSessionProcessor processor,
        NetworkPipeline pipeline)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.Processor = processor ?? throw new ArgumentNullException(nameof(processor));
        this.Pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
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

    private IProtocolSessionProcessor Processor
    {
        get;
    }

    private NetworkPipeline Pipeline
    {
        get;
    }

    /// <summary>
    /// Serializes semantic execution against the runtime.
    /// Ensures protocol state is single-threaded even though
    /// multiple driver loops are running.
    /// </summary>
    private SemaphoreSlim ProcessorGate
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
    /// Starts executing the driver loops. May be called once.
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
    // Transport read loop (bytes -> frames)
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
        var pipeline = this.Pipeline;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                int bytesRead;
                try
                {
                    bytesRead = await pipeline
                        .ReadBytesAsync(buffer, ct)
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
                    await pipeline
                        .CompleteDecodingAsync(ct)
                        .ConfigureAwait(false);
                    return;
                }

                var sequence = new ReadOnlySequence<byte>(
                    buffer.AsMemory(0, bytesRead));

                this.Logger.LogDebug("[DRIVER READ LOOP] calling DecodeFrameAsync");
                await pipeline
                    .DecodeFrameAsync(sequence, ct)
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
    // Outbound write loop (session -> transport)
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
            await this.Processor
                .WaitForOutboundFrameAsync(ct)
                .ConfigureAwait(false);

            ProtocolFrame? protocolFrame;

            await this.ProcessorGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (!this.Processor.TryDequeueOutboundFrame(out protocolFrame))
                {
                    protocolFrame = null;
                }
            }
            finally
            {
                this.ProcessorGate.Release();
            }

            if (protocolFrame is null)
            {
                await Task.Yield();
                continue;
            }

#if ENABLE_PROTOCOL_FRAME_DIAGNOSTICS
            this.RecentOutboundFrames.Write(protocolFrame);
#endif

            var networkFrame = FrameConverter.ToNetworkFrame(protocolFrame);

            try
            {
                await this.Pipeline
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
    // Frame consumption loop (frames -> session)
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
            var networkFrame = await this.Pipeline
                .ReadFrameAsync(ct)
                .ConfigureAwait(false);

            var protocolFrame = FrameConverter.ToProtocolFrame(networkFrame);

#if ENABLE_PROTOCOL_FRAME_DIAGNOSTICS
            protocolFrame.Diagnostics.ReceivedTimestamp = Stopwatch.GetTimestamp();
            this.RecentInboundFrames.Write(protocolFrame);
#endif

            await this.ProcessorGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                this.Processor.ProcessFrame(protocolFrame);
            }
            finally
            {
                this.ProcessorGate.Release();
            }
        }
    }
}
