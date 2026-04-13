using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests;

namespace MWB.Networking.Layer2_Protocol;

public sealed partial class ProtocolSession : IProtocolSession
{
    // ------------------------------------------------------------------
    // Request handling - Outbound
    // ------------------------------------------------------------------

    public void SendRequest(ReadOnlyMemory<byte> payload)
    {
        // Generate a new unique request ID<br>
        var requestId = this.NextRequestId++;
        // Create and track request context
        var context = new RequestContext(requestId);
        this.RequestContexts.Add(requestId, context);
        // Emit the protocol request frame to the peer
        this.EnqueueOutboundFrame(ProtocolFrames.Request(requestId, payload));
    }

    internal void CloseRequestWithResponse(
        RequestContext context,
        ReadOnlyMemory<byte> payload)
    {
        // 1. Transition lifecycle (validation happens here)
        context.Close();

        // 2. Emit terminal response frame
        this.EnqueueOutboundFrame(ProtocolFrames.Response(context.RequestId, payload));

        // 3. Remove request + close any request-scoped streams
        this.RemoveRequest(context.RequestId);
    }

    internal void CloseRequestWithError(
        RequestContext context,
        ReadOnlyMemory<byte> payload)
    {
        context.Close();

        this.EnqueueOutboundFrame(ProtocolFrames.Error(context.RequestId, payload));

        this.RemoveRequest(context.RequestId);
    }
}
