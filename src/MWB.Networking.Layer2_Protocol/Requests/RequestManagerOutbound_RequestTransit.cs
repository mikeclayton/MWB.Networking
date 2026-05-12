using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Requests.Internal;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed partial class RequestManagerOutbound
{
    // ------------------------------------------------------------
    // Outgoing Request / Response ingress
    // (application → adapter → session → adapter → driver)
    //  ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
    // ------------------------------------------------------------

    /// <summary>
    /// Consumes a locally generated outgoing request.
    /// </summary>
    internal Request ConsumeOutgoingRequest(
        uint? requestType,
        ReadOnlyMemory<byte> payload)
    {
        // Generate a new unique request ID
        // (thread-safe, overflow-safe incrementing)
        var requestId = this.GetNextRequestId();

        // Create and track request context
        var context = new RequestContext(requestId, requestType, ProtocolDirection.Outgoing);
        var outgoingRequest = new OutgoingRequest(context, this.RequestManager.Actions);
        var requestEntry = new RequestEntry(context, outgoingRequest);
        this.RequestEntries.AddRequestEntry(requestEntry);

        // transmit the protocol request frame to the remote peer
        var request = outgoingRequest.AsPublishable(payload);
        this.TransmitOutgoingRequest(request);
        return request;
    }

    // ------------------------------------------------------------
    // Outgoing Request / Response egress
    // (application → adapter → session → adapter → driver)
    //                          ^^^^^^^^^^^^^^^^^^^^^^^^^^
    // ------------------------------------------------------------

    /// <summary>
    /// Transmits an outgoing request for delivery to the remote peer.
    /// </summary>
    internal void TransmitOutgoingRequest(Request request)
    {
        ArgumentNullException.ThrowIfNull(request);

        this.Logger.LogTrace(
            "Transmitting outgoing request (Id={RequestId})",
            request.RequestId);

        this.RequestManager.Session.OutgoingActionSink.TransmitOutgoingRequest(request);
    }
}
