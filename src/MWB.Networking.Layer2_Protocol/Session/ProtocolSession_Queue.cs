using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Requests;
using System.Diagnostics;

namespace MWB.Networking.Layer2_Protocol.Session;

public sealed partial class ProtocolSession
{
    // ------------------------------------------------------------------
    // Outbound queue coordination
    // ------------------------------------------------------------------

    /// <summary>
    /// Thread-safe outbound frame queue. Enqueue/dequeue operations are
    /// internally synchronized - callers do not need to coordinate access.
    /// </summary>
    private OutboundFrameQueue OutboundFrames
    {
        get;
    } = new();

    internal Task WaitForOutboundFrameAsync(CancellationToken ct)
    {
        return this.OutboundFrames.WaitForFrameAsync(ct);
    }

    internal void EnqueueOutboundFrame(ProtocolFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        // --------------------------------------------------------------
        // Validate Request-scoped frames
        // --------------------------------------------------------------
        if (frame.RequestId is not null)
        {
            if (!this.RequestManager.TryGetRequestContext(
                    frame.RequestId.Value,
                    out var requestContext))
            {
                throw ProtocolException.InvalidFrameSequence(
                    frame,
                    "Unknown or completed RequestId");
            }

            // Ensure the Request is still open
            if (!RequestManager.IsTerminalRequestFrame(frame))
            {
                requestContext.EnsureOpen();
            }
        }

        // --------------------------------------------------------------
        // Validate Stream-scoped frames
        // --------------------------------------------------------------
        if (frame.StreamId is not null)
        {
            if (!this.StreamManager.TryGetStreamEntry(
                    frame.StreamId.Value,
                    out var streamEntry))
            {
                throw ProtocolException.InvalidFrameSequence(
                    frame,
                    "Unknown StreamId");
            }

            if (streamEntry.Context.IsRequestScoped)
            {
                streamEntry.Context
                    .OwningRequest
                    .EnsureOpen();
            }
        }

        // --------------------------------------------------------------
        // Enqueue outbound frame
        // --------------------------------------------------------------
#if ENABLE_PROTOCOL_FRAME_DIAGNOSTICS
        frame.Diagnostics.EnqueuedTimestamp =
            Stopwatch.GetTimestamp();
#endif

        this.OutboundFrames.Enqueue(frame);
    }
}
