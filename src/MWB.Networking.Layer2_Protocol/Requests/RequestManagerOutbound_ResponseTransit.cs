using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Requests.Internal;

namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed partial class RequestManagerOutbound
{
    // ------------------------------------------------------------
    // Outgoing Request / Response ingress
    // (application → adapter → session → adapter → driver)
    //  ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
    // ------------------------------------------------------------

    /// <summary>
    /// Consumes a locally generated outgoing response.
    /// </summary>
    internal Response ConsumeOutgoingResponse(
        uint requestId,
        uint? responseType,
        ReadOnlyMemory<byte> payload)
    {
        // Ensure the request exists
        var requestEntry = this.RequestEntries.EnsureRequestExists(requestId);

        // Only respond to incoming requests
        if (!requestEntry.IsIncoming)
        {
            throw ProtocolException.InvalidSequence(
                "Cannot send a response for a request initiated by the local peer.");
        }

        // This is a successful (non-error) terminal response.
        // Error responses are emitted via the separate error-handling path.
        var outgoingResponse = new OutgoingResponse(requestId, responseType, isError:false);

        // Close the request from the outbound side
        requestEntry.Context.Close();

        this.RequestManager.RemoveRequest(requestId);

        // Commit the response
        var response = outgoingResponse.AsPublishable(payload);
        this.TransmitOutgoingResponse(response);
        return response;
    }

    // ------------------------------------------------------------
    // Outgoing Request / Response egress
    // (application → adapter → session → adapter → driver)
    //                          ^^^^^^^^^^^^^^^^^^^^^^^^^^
    // ------------------------------------------------------------

    /// <summary>
    /// Transmits an outgoing response for delivery to the remote peer.
    /// </summary>
    internal void TransmitOutgoingResponse(Response response)
    {
        ArgumentNullException.ThrowIfNull(response);

        this.Logger.LogTrace(
            "Transmitting outgoing response (Id={RequestId})",
            response.RequestId);

        this.RequestManager.Session.OutgoingActionSink.TransmitOutgoingResponse(response);
    }
}
