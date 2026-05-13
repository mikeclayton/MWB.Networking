using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests.Api;

namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed partial class RequestManager
{
    // ------------------------------------------------------------
    // Incoming Request / Response ingress
    // (driver → adapter → session → adapter → application)
    //  ^^^^^^^^^^^^^^^^^^^^^^^^^^
    // ------------------------------------------------------------

    /// <summary>
    /// Consumes an incoming response from a remote peer.
    /// </summary>
    internal IncomingResponse ConsumeIncomingResponse(
        uint requestId,
        uint? responseType,
        ReadOnlyMemory<byte> payload,
        bool isError)
    {
        // make sure the request being responded to exists
        var requestContext = this.RequestContexts.GetOrThrow(requestId);

        // A Response or Error frame must only close a request that *we* initiated
        // (an outgoing request). If the ID matches a request the peer opened (an
        // incoming entry), the frame is a protocol violation: the peer is trying to
        // respond to their own request, which makes no sense.
        if (requestContext.Direction != ProtocolDirection.Outgoing)
        {
            throw ProtocolException.InvalidSequence(
                "Response or Error frame targets an incoming request, not an outgoing one.");
        }

        // create the public api response
        var incomingResponse = new IncomingResponse(requestId, responseType, payload, isError);

        // Close the request based on a terminal frame received from the peer.
        requestContext.CompleteWithResponse(incomingResponse);

        // Remove the request lifecycle entry before transmitting the response
        // to prevent re-entrant lookup during transmission
        this.RequestContexts.Remove(requestContext.RequestId);

        // publish to sinks
        this.PublishIncomingResponse(incomingResponse);
        return incomingResponse;
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
    internal void PublishIncomingResponse(IncomingResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        this.Logger.LogTrace(
            "Publishing incoming response (Id={RequestId})",
            response.RequestId);

        this.Session.IncomingActionSink.PublishIncomingResponse(response);
    }
}
