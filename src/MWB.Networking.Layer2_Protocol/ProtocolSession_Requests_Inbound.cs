using MWB.Networking.Layer2_Protocol.Requests;

namespace MWB.Networking.Layer2_Protocol;

public sealed partial class ProtocolSession : IProtocolSession
{
    // ------------------------------------------------------------------
    // Request handling - Inbound
    // ------------------------------------------------------------------

    private void ProcessInboundRequestFrame(ProtocolFrame frame)
    {
        if (frame.RequestId is null)
        {
            throw ProtocolError(frame, "Request frame missing RequestId");
        }

        var requestId = frame.RequestId.Value;

        if (this.RequestContexts.ContainsKey(requestId))
        {
            throw ProtocolError(frame, "Duplicate RequestId");
        }

        var context = new RequestContext(requestId);
        this.RequestContexts.Add(requestId, context);

        var request = new IncomingRequest(this, context);

        this.RaiseRequestReceived(request, frame.Payload);
    }

    private void ProcessInboundResponseFrame(ProtocolFrame frame)
    {
        if (frame.RequestId is null)
        {
            throw ProtocolError(frame, "Response frame missing RequestId");
        }

        if (!this.RequestContexts.TryGetValue(frame.RequestId.Value, out var context))
        {
            throw ProtocolError(frame, "Unknown or completed RequestId");
        }

        // Close the Request based on a terminal frame received from the peer.
        // This MUST NOT emit any protocol frames.
        context.CloseFromInbound(frame);

        // Remove the request (terminal)
        this.RequestContexts.Remove(frame.RequestId.Value);
    }
}
