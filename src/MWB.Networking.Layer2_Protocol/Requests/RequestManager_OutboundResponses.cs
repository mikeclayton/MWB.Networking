using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests.Api;

namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed partial class RequestManager
{
    // ------------------------------------------------------------
    // Outgoing Request / Response ingress
    // (application → adapter → session → adapter → driver)
    //  ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
    // ------------------------------------------------------------

    /// <summary>
    /// Consumes a locally generated outgoing response.
    /// </summary>
    internal OutgoingResponse ConsumeOutgoingResponse(
        uint requestId,
        uint? responseType,
        ReadOnlyMemory<byte> payload,
        bool isError)
    {
        // make sure the request being responded to exists
        var requestContext = this.RequestContexts.GetOrThrow(requestId);

        // only respond to incoming requests
        if (requestContext.Direction != ProtocolDirection.Incoming)
        {
            throw ProtocolException.InvalidSequence(
                "Cannot send a response for a request initiated by the local peer.");
        }

        // create the public api response
        var outgoingResponse = new OutgoingResponse(requestId, responseType, payload, isError);

        // close the request from the outbound side
        requestContext.Close();
        this.RemoveRequest(requestContext);

        // publish to sinks
        this.TransmitOutgoingResponse(outgoingResponse);
        return outgoingResponse;
    }

    // ------------------------------------------------------------
    // Outgoing Request / Response egress
    // (application → adapter → session → adapter → driver)
    //                          ^^^^^^^^^^^^^^^^^^^^^^^^^^
    // ------------------------------------------------------------

    /// <summary>
    /// Transmits an outgoing response for delivery to the remote peer.
    /// </summary>
    internal void TransmitOutgoingResponse(OutgoingResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        this.Logger.LogTrace(
            "Transmitting outgoing response (Id={RequestId})",
            response.RequestId);

        this.Session.OutgoingActionSink.TransmitOutgoingResponse(response);
    }
}
