using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Requests.Internal;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed partial class RequestManagerInbound
{
    // ------------------------------------------------------------
    // Incoming Request / Response ingress
    // (driver → adapter → session → adapter → application)
    //  ^^^^^^^^^^^^^^^^^^^^^^^^^^
    // ------------------------------------------------------------

    /// <summary>
    /// Consumes an incoming request from a remote peer.
    /// </summary>
    internal Request ConsumeIncomingRequest(
        uint requestId,
        uint? requestType,
        ReadOnlyMemory<byte> payload)
    {
        // prevent duplicate request ids
        this.RequestEntries.EnsureRequestDoesNotExist(requestId);

        // add a new entry to track the incoming request lifecycle
        var requestContext = new RequestContext(requestId, requestType, ProtocolDirection.Incoming);
        var incomingRequest = new IncomingRequest(requestContext, this.RequestManager.Actions);
        var requestEntry = new RequestEntry(requestContext, incomingRequest);
        this.RequestEntries.AddRequestEntry(requestEntry);

        var request = incomingRequest.AsPublishable(payload);
        this.PublishIncomingRequest(request);
        return request;
    }

    // ------------------------------------------------------------
    // Incoming Request / Response egress
    // (driver → adapter → session → adapter → application)
    //                     ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
    // ------------------------------------------------------------

    /// <summary>
    /// Publishes an incoming request for delivery to the local application.
    /// </summary>
    /// <remarks>
    /// Called by the request state machine after request semantics
    /// have been fully processed.
    /// </remarks>
    internal void PublishIncomingRequest(Request request)
    {
        ArgumentNullException.ThrowIfNull(request);

        this.Logger.LogTrace(
            "Publishing incoming request (Id={RequestId})",
            request.RequestId);

        this.RequestManager.Session.IncomingActionSink.PublishIncomingRequest(request);
    }
}
