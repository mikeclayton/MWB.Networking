using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;
using MWB.Networking.Layer2_Protocol.Session;

namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed class RequestManagerInbound
{
    internal RequestManagerInbound(ProtocolSession session, RequestManager requestManager, RequestContexts requestContexts)
    {
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.RequestManager = requestManager ?? throw new ArgumentNullException(nameof(requestManager));
        this.RequestContexts = requestContexts ?? throw new ArgumentNullException(nameof(requestContexts));
    }

    private ProtocolSession Session
    {
        get;
    }

    private RequestManager RequestManager
    {
        get;
    }

    private RequestContexts RequestContexts
    {
        get;
    }

    private IncomingRequests IncomingRequests
    {
        get;
    } = new();

    // ------------------------------------------------------------------
    // Utility methods
    // ------------------------------------------------------------------

    private void EnsureFrameHasRequestId(
        ProtocolFrame frame,
        out uint requestId)
    {
        requestId = frame.RequestId
            ?? throw ProtocolException.ProtocolViolation(
                frame,
                $"{nameof(ProtocolFrame)} with {nameof(ProtocolFrame.Kind)} of {nameof(ProtocolFrameKind)} must have a {nameof(ProtocolFrame.RequestId)}");
    }


    private void EnsureRequestContextDoesNotExist(
        ProtocolFrame frame,
        uint requestId)
    {
        if (this.RequestContexts.RequestContextExists(requestId))
        {
            throw ProtocolException.InvalidFrameSequence(
                frame, "Duplicate RequestId");
        }
    }

    private void EnsureRequestContextExists(
        ProtocolFrame frame,
        uint requestId,
        out RequestContext requestContext)
    {
        if (this.RequestContexts.TryGetRequestContext(requestId, out var result))
        {
            requestContext = result;
            return;
        }
        throw ProtocolException.InvalidFrameSequence(
            frame, "Unknown or completed RequestId");
    }

    // ------------------------------------------------------------------
    // Incoming Request wrappers
    // ------------------------------------------------------------------

    internal void RemoveIncomingRequest(RequestContext context)
    {
        this.IncomingRequests.RemoveIncomingRequest(context);
    }

    // ------------------------------------------------------------------
    // Request handling - Inbound
    // ------------------------------------------------------------------

    internal void ProcessInboundRequestFrame(ProtocolFrame frame)
    {
        this.EnsureFrameHasRequestId(frame, out var requestId);
        this.EnsureRequestContextDoesNotExist(frame, requestId);

        var requestType = frame.RequestType;

        var requestContext = new RequestContext(requestId, requestType);
        this.RequestContexts.AddRequestContext(requestContext);

        var request = new IncomingRequest(this.Session, requestContext);
        this.IncomingRequests.AddIncomingRequest(request);

        this.Session.OnRequestReceived(request, frame.Payload);
    }

    internal void ProcessInboundResponseFrame(ProtocolFrame frame)
    {
        this.EnsureFrameHasRequestId(frame, out var requestId);
        this.EnsureRequestContextExists(frame, requestId, out var requestContext);

        // Close the Request based on a terminal frame received from the peer.
        // This MUST NOT emit any protocol frames.
        requestContext.CloseFromInbound(frame);

        // Tear down all request-scoped streams
        this.Session.StreamManager.TearDownRequestStreams(requestId);

        // Remove the request (terminal)
        this.RequestContexts.RemoveRequestContext(requestId);
    }
}
