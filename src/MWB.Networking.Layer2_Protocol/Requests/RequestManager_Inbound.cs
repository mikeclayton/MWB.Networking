namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed partial class RequestManager
{
    // ------------------------------------------------------------------
    // Request handling - Inbound
    // ------------------------------------------------------------------

    internal void ProcessInboundRequestFrame(ProtocolFrame frame)
    {
        if (frame.RequestId is null)
        {
            throw ProtocolException.InvalidFrameSequence(frame, "Request frame missing RequestId");
        }

        var requestId = frame.RequestId.Value;

        if (this.RequestContexts.ContainsKey(requestId))
        {
            throw ProtocolException.InvalidFrameSequence(frame, "Duplicate RequestId");
        }

        var context = new RequestContext(requestId);
        this.RequestContexts.Add(requestId, context);

        var request = new IncomingRequest(this.Session, context);

        this.Session.OnRequestReceived(request, frame.Payload);
    }

    internal void ProcessInboundResponseFrame(ProtocolFrame frame)
    {
        if (frame.RequestId is null)
        {
            throw ProtocolException.InvalidFrameSequence(frame, "Response frame missing RequestId");
        }

        if (!this.RequestContexts.TryGetValue(frame.RequestId.Value, out var context))
        {
            throw ProtocolException.InvalidFrameSequence(frame, "Unknown or completed RequestId");
        }

        // Close the Request based on a terminal frame received from the peer.
        // This MUST NOT emit any protocol frames.
        context.CloseFromInbound(frame);

        // Remove the request (terminal)
        this.RequestContexts.Remove(frame.RequestId.Value);
    }
}
