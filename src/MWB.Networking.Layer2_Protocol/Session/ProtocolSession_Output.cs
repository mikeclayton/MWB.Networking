using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Requests;
using System.Diagnostics;

namespace MWB.Networking.Layer2_Protocol.Session;

public sealed partial class ProtocolSession : IProtocolSessionOutput
{
    // ------------------------------------------------------------------
    // Outbound frame event
    // ------------------------------------------------------------------

    private Action<ProtocolFrame>? _outboundFrameReady;

    /// <summary>
    /// Explicit implementation of <see cref="IProtocolSessionOutput.OutboundFrameReady"/>.
    ///
    /// This member is implemented explicitly to keep the frame-level session API
    /// intentionally less visible in the public surface of <see cref="ProtocolSession"/>.
    /// It is intended for infrastructure components (e.g. SessionAdapter), not for
    /// application-level protocol usage.
    /// </summary>
    event Action<ProtocolFrame> IProtocolSessionOutput.OutboundFrameReady
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
        // send outbound frame
        // --------------------------------------------------------------
#if ENABLE_PROTOCOL_FRAME_DIAGNOSTICS
        frame.Diagnostics.SentTimestamp =
            Stopwatch.GetTimestamp();
#endif

        this.RaiseOutboundFrameReady(frame);
    }
}
