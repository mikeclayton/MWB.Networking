using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Pipeline;
using MWB.Networking.Layer2_Protocol.Adapter;
using MWB.Networking.Layer2_Protocol.Session.Frames;
using MWB.Networking.Logging;
using System.Buffers;
using System.Diagnostics;

namespace MWB.Networking.Layer2_Protocol.Adapter;

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
public sealed partial class SessionAdapter
{
    // ------------------------------------------------------------------
    // Execution
    // ------------------------------------------------------------------

    private async Task RunInternalAsync(CancellationToken ct)
    {
        using var scope = this.Logger.BeginMethodLoggingScope(this);

        var readTask = this.RunReadLoopAsync(ct);
        var writeTask = this.RunWriteLoopAsync(ct);
        var consumeTask = this.ConsumeFramesAsync(ct);

        this.SignalStarted();

        await Task
            .WhenAny(
                readTask,
                writeTask,
                consumeTask)
            .ConfigureAwait(false);
    }

    // ------------------------------------------------------------------
    // Transport read loop - pipeline (bytes) -> session (frames)
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
    // Outbound write loop - session (frames) -> pipeline (bytes)
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
