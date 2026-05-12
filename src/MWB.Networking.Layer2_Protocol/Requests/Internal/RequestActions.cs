using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;
using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Requests.Internal;

internal sealed class RequestActions
{
    internal RequestActions(RequestManager requestManager)
    {
        this.RequestManager = requestManager ?? throw new ArgumentNullException(nameof(requestManager));
    }

    private RequestManager RequestManager
    {
        get;
    }

    internal Response Respond(
        RequestContext context,
        uint? responseType,
        ReadOnlyMemory<byte> payload)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Direction != ProtocolDirection.Incoming)
        {
            throw new InvalidOperationException("Cannot respond to an outbound request.");
        }

        var response = this.RequestManager.Outbound.ConsumeOutgoingResponse(
            context.RequestId, responseType, payload);

        return response;
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
