using MWB.Networking.Layer2_Protocol.Internal;

namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed partial class RequestManager
{
    // ------------------------------------------------------------------
    // Request handling - Outbound
    // ------------------------------------------------------------------

    public OutgoingRequest SendRequest(ReadOnlyMemory<byte> payload)
    {
        // Generate a new unique request ID<br>
        var requestId = this.NextRequestId++;

        // Create and track request context
        var context = new RequestContext(requestId);
        this.RequestContexts.Add(requestId, context);

        // Emit the protocol request frame to the peer
        this.Session.EnqueueOutboundFrame(ProtocolFrames.Request(requestId, payload));

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
        this.Session.EnqueueOutboundFrame(ProtocolFrames.Response(context.RequestId, payload));

        // 3. Remove request + close any request-scoped streams
        this.RemoveRequest(context.RequestId);
    }

    internal void CloseRequestWithError(
        RequestContext context,
        ReadOnlyMemory<byte> payload)
    {
        context.Close();

        this.Session.EnqueueOutboundFrame(ProtocolFrames.Error(context.RequestId, payload));

        this.RemoveRequest(context.RequestId);
    }
}
