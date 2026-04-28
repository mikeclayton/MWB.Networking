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
        if (frame.RequestId is null)
        {
            throw ProtocolException.InvalidFrameSequence(frame, "Request frame missing RequestId");
        }

        var requestId = frame.RequestId.Value;
        if (this.RequestContexts.RequestContextExists(requestId))
        {
            throw ProtocolException.InvalidFrameSequence(frame, "Duplicate RequestId");
        }

        var requestType = frame.RequestType;

        var context = new RequestContext(requestId, requestType);
        this.RequestContexts.AddRequestContext(context);

        var request = new IncomingRequest(this.Session, context);
        this.IncomingRequests.AddIncomingRequest(request);

        this.Session.OnRequestReceived(request, frame.Payload);
    }

    internal void ProcessInboundResponseFrame(ProtocolFrame frame)
    {
        if (frame.RequestId is null)
        {
            throw ProtocolException.InvalidFrameSequence(frame, "Response frame missing RequestId");
        }

        if (!this.RequestContexts.TryGetRequestContext(frame.RequestId.Value, out var context))
        {
            throw ProtocolException.InvalidFrameSequence(frame, "Unknown or completed RequestId");
        }

        // Close the Request based on a terminal frame received from the peer.
        // This MUST NOT emit any protocol frames.
        context.CloseFromInbound(frame);

        // Remove the request (terminal)
        this.RequestContexts.RemoveRequestContext(frame.RequestId.Value);
    }
}
