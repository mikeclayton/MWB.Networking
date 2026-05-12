using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Requests.Api;
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
    internal void ConsumeOutgoingRequest(
        uint? requestType,
        ReadOnlyMemory<byte> payload)
    {
        // Generate a new unique request ID<br>
        var requestId = this.NextRequestId++;

        // Create and track request context
        var context = new RequestContext(requestId, requestType);
        var outgoingRequest = new OutgoingRequest(this.RequestManager, context);
        var requestEntry = new RequestEntry(context, outgoingRequest);
        this.RequestEntries.AddRequestEntry(requestEntry);

        // Emit the protocol request frame to the local peer
        this.TransmitOutgoingRequest(outgoingRequest, payload);
    }

    // ------------------------------------------------------------
    // Outgoing Request / Response egress
    // (application → adapter → session → adapter → driver)
    //                          ^^^^^^^^^^^^^^^^^^^^^^^^^^
    // ------------------------------------------------------------

    /// <summary>
    /// Transmits an outgoing request for delivery to the remote peer.
    /// </summary>
    internal void TransmitOutgoingRequest(
        OutgoingRequest request,
        ReadOnlyMemory<byte> payload)
    {
        ArgumentNullException.ThrowIfNull(request);

        this.Logger.LogTrace(
            "Transmitting outgoing request (Id={RequestId})",
            request.RequestId);

        this.RequestManager.Session.OutgoingActionSink.TransmitOutgoingRequest(request, payload);
    }
}
