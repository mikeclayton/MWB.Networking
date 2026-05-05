using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;
using MWB.Networking.Layer2_Protocol.Session;

namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed class RequestManagerOutbound
{
    internal RequestManagerOutbound(ProtocolSession session, RequestManager requestManager, RequestContexts requestContexts)
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

    private uint NextRequestId
    {
        get;
        set;
    } = 1;

    // ------------------------------------------------------------------
    // Request handling - Outbound
    // ------------------------------------------------------------------

    internal OutgoingRequest SendRequest(uint? requestType, ReadOnlyMemory<byte> payload)
    {
        // Generate a new unique request ID<br>
        var requestId = this.NextRequestId++;

        // Create and track request context
        var context = new RequestContext(requestId, requestType);
        this.RequestContexts.AddRequestContext(context);

        // Emit the protocol request frame to the peer
        this.Session.SendOutboundFrame(
            ProtocolFrames.Request(requestId, requestType, payload));

        // Return an application-facing handle
        return new OutgoingRequest(this.Session, context);
    }

    internal void CloseRequestWithResponse(
        RequestContext context,
        ReadOnlyMemory<byte> payload)
    {
        // 1. Transition lifecycle (validation happens here)
        context.Close();

        // 2. Emit terminal response frame
        this.Session.SendOutboundFrame(ProtocolFrames.Response(context.RequestId, null, payload));

        // 3. Remove request + close any request-scoped streams
        this.RequestManager.RemoveRequest(context.RequestId);
    }

    internal void CloseRequestWithError(
        RequestContext context,
        ReadOnlyMemory<byte> payload)
    {
        context.Close();

        this.Session.SendOutboundFrame(ProtocolFrames.Error(context.RequestId, payload));

        this.RequestManager.RemoveRequest(context.RequestId);
    }
}
