using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Requests;
using System.Diagnostics;

namespace MWB.Networking.Layer2_Protocol.Session;

internal sealed partial class ProtocolSession
{
    // ------------------------------------------------------------------
    // Outbound frame event
    // ------------------------------------------------------------------

    private Action<ProtocolFrame>? _outboundFrameReady;

    internal event Action<ProtocolFrame> OutboundFrameReady
    {
        add => _outboundFrameReady += value;
        remove => _outboundFrameReady -= value;
    }

    private void RaiseOutboundFrameReady(ProtocolFrame frame)
    {
        _outboundFrameReady?.Invoke(frame);
    }

    // ------------------------------------------------------------------
    // Outbound frame coordination
    // ------------------------------------------------------------------

    internal void SendOutboundFrame(ProtocolFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        // --------------------------------------------------------------
        // Validate Request-scoped frames
        // --------------------------------------------------------------
        if (frame.RequestId is not null)
        {
            if (!this.RequestManager.TryGetRequestEntry(
                frame.RequestId.Value,
                out var requestEntry))
            {
                throw ProtocolException.InvalidFrameSequence(
                    frame,
                    "Unknown or completed RequestId");
            }

            // Ensure the Request is still open
            if (!RequestManager.IsTerminalRequestFrame(frame))
            {
                requestEntry.Context.EnsureOpen();
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
        // send outbound frame
        // --------------------------------------------------------------
#if ENABLE_PROTOCOL_FRAME_DIAGNOSTICS
        frame.Diagnostics.SentTimestamp =
            Stopwatch.GetTimestamp();
#endif

        this.RaiseOutboundFrameReady(frame);
    }
}
