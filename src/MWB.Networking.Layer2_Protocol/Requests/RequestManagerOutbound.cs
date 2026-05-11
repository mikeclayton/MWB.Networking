using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed class RequestManagerOutbound
{
    internal RequestManagerOutbound(
        RequestManager requestManager,
        RequestEntries requestEntries)
    {
        this.RequestManager = requestManager ?? throw new ArgumentNullException(nameof(requestManager));
        this.RequestEntries = requestEntries ?? throw new ArgumentNullException(nameof(requestEntries));
    }

    private RequestManager RequestManager
    {
        get;
    }

    private RequestEntries RequestEntries
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

    internal OutgoingRequest SendRequest(uint? requestType = null, ReadOnlyMemory<byte> payload = default)
    {
        // Generate a new unique request ID<br>
        var requestId = this.NextRequestId++;

        // Create and track request context
        var context = new RequestContext(requestId, requestType);
        var outgoingRequest = new OutgoingRequest(context);
        var requestEntry = new RequestEntry(context, outgoingRequest);
        this.RequestEntries.AddRequestEntry(requestEntry);

        // Emit the protocol request frame to the peer
        this.RequestManager.EmitOutgoingRequest(outgoingRequest, payload);

        // Return an application-facing handle
        return outgoingRequest;
    }

    // ------------------------------------------------------------------
    // IRequestResponder
    // ------------------------------------------------------------------
    private OutgoingResponse Respond(RequestContext context, uint? responseType, ReadOnlyMemory<byte> payload)
    {
        // 1. Transition lifecycle (validation happens here)
        context.Close();

        // 2. Emit terminal response frame
        this.Session.SendOutboundFrame(
            ProtocolFrames.Response(context.RequestId, responseType, payload));

        // 3. Remove request + close any request-scoped streams
        this.RequestManager.RemoveRequest(context.RequestId);

        return new OutgoingResponse(context.RequestId, responseType, isError: false);
    }

    private OutgoingResponse Reject(RequestContext context, ReadOnlyMemory<byte> payload)
    {
        context.Close();

        this.Session.SendOutboundFrame(ProtocolFrames.Error(context.RequestId, payload));

        this.RequestManager.RemoveRequest(context.RequestId);

        return new OutgoingResponse(context.RequestId, responseType: null, isError: true);
    }
}
