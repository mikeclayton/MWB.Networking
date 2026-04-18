using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed partial class RequestManager
{
    // ------------------------------------------------------------------
    // IncomingRequest cache - only use methods, don't access the field directly
    // ------------------------------------------------------------------

    private readonly Dictionary<RequestContext, IncomingRequest>
        _cachedIncomingRequests = [];

    private void AddCachedIncomingRequest(IncomingRequest request)
    {
        _cachedIncomingRequests.Add(request.Context, request);
    }

    internal IncomingRequest? GetCachedIncomingRequest(RequestContext? context)
    {
        // Null means this is a session-scoped stream or operation
        if (context is null)
        {
            return null;
        }

        // There must be exactly one IncomingRequest per RequestContext.
        // If this lookup fails, it indicates a protocol or lifecycle bug.
        if (!_cachedIncomingRequests.TryGetValue(context, out var incomingRequest))
        {
            throw new InvalidOperationException(
                "RequestContext has no associated IncomingRequest. " +
                "This indicates an internal request lifecycle inconsistency.");
        }

        return incomingRequest;
    }

    private void RemoveCachedIncomingRequest(RequestContext context)
    {
        _cachedIncomingRequests.Remove(context);
    }

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

        if (this.RequestContextExists(requestId))
        {
            throw ProtocolException.InvalidFrameSequence(frame, "Duplicate RequestId");
        }

        var context = new RequestContext(requestId);
        this.AddRequestContext(context);

        var request = new IncomingRequest(this.Session, context);
        this.AddCachedIncomingRequest(request);

        this.Session.OnRequestReceived(request, frame.Payload);
    }

    internal void ProcessInboundResponseFrame(ProtocolFrame frame)
    {
        if (frame.RequestId is null)
        {
            throw ProtocolException.InvalidFrameSequence(frame, "Response frame missing RequestId");
        }

        if (!this.TryGetRequestContext(frame.RequestId.Value, out var context))
        {
            throw ProtocolException.InvalidFrameSequence(frame, "Unknown or completed RequestId");
        }

        // Close the Request based on a terminal frame received from the peer.
        // This MUST NOT emit any protocol frames.
        context.CloseFromInbound(frame);

        // Remove the request (terminal)
        this.RemoveRequestContext(frame.RequestId.Value);
    }
}
