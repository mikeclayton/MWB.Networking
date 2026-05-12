using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;
using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed class IncomingRequestActions
{
    internal IncomingRequestActions(RequestManager requestManager)
    {
        this.RequestManager = requestManager ?? throw new ArgumentNullException(nameof(requestManager));
    }

    private RequestManager RequestManager
    {
        get;
    }

    internal OutgoingResponse Respond(
        RequestContext context,
        uint? responseType,
        ReadOnlyMemory<byte> payload)
    {
        ArgumentNullException.ThrowIfNull(context);

        var outgoingResponse = this.RequestManager.Outbound.ConsumeOutgoingResponse(
            context.RequestId, responseType, payload);

        return outgoingResponse;
    }

    internal OutgoingStream OpenRequestStream(
        RequestContext context,
        uint? streamType)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.OpenStream();

        return this.RequestManager.Session.StreamManager.Outbound
            .OpenRequestStream(streamType, context);
    }
}
