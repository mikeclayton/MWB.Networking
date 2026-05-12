using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Requests.Internal;

namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed partial class RequestManagerInbound
{
    // ------------------------------------------------------------
    // Incoming Request / Response ingress
    // (driver → adapter → session → adapter → application)
    //  ^^^^^^^^^^^^^^^^^^^^^^^^^^
    // ------------------------------------------------------------

    /// <summary>
    /// Consumes an incoming response from a remote peer.
    /// </summary>
    internal Response ConsumeIncomingResponse(
        uint requestId,
        uint? responseType,
        bool isError,
        ReadOnlyMemory<byte> payload)
    {
        // make sure the request being responded to exists
        var requestEntry = this.RequestEntries.EnsureRequestExists(requestId);

        // A Response or Error frame must only close a request that *we* initiated
        // (an outgoing request). If the ID matches a request the peer opened (an
        // incoming entry), the frame is a protocol violation: the peer is trying to
        // respond to their own request, which makes no sense.
        if (!requestEntry.IsOutgoing)
        {
            throw ProtocolException.InvalidSequence(
                "Response or Error frame targets an incoming request, not an outgoing one.");
        }

        var incomingResponse = new IncomingResponse(requestId, responseType, isError);

        // Close the request based on a terminal frame received from the peer.
        requestEntry.Context.CloseFromInbound(incomingResponse);

        this.RequestManager.RemoveRequest(requestId);

        var response = incomingResponse.AsPublishable(payload);
        this.PublishIncomingResponse(response);
        return response;
    }

    // ------------------------------------------------------------
    // Incoming Request / Response egress
    // (driver → adapter → session → adapter → application)
    //                     ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
    // ------------------------------------------------------------

    /// <summary>
    /// Publishes an incoming response for delivery to the application.
    /// </summary>
    /// <remarks>
    /// Called by the request state machine after response semantics
    /// have been fully processed.
    /// </remarks>
    internal void PublishIncomingResponse(Response response)
    {
        ArgumentNullException.ThrowIfNull(response);

        this.Logger.LogTrace(
            "Publishing incoming response (Id={RequestId})",
            response.RequestId);

        this.RequestManager.Session.IncomingActionSink.PublishIncomingResponse(response);
    }
}
