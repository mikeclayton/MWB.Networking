using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests.Api;

namespace MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

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

    internal OutgoingResponse Respond(
        RequestContext requestContext,
        uint? responseType,
        ReadOnlyMemory<byte> payload,
        bool isError)
    {
        // ordering: validate -> transition -> execute

        // validate the current state
        ArgumentNullException.ThrowIfNull(requestContext);
        requestContext.EnsureCanRespond();
        if (requestContext.Direction != ProtocolDirection.Incoming)
        {
            throw new InvalidOperationException("Cannot respond to an outgoing request.");
        }

        // transition to the next state
        requestContext.Respond();

        // execute the external protocol work
        var response = this.RequestManager.ConsumeOutgoingResponse(
            requestContext.RequestId, responseType, payload, isError);

        return response;
    }
}
