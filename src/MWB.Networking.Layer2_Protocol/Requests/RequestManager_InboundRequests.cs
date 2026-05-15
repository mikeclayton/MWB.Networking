using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed partial class RequestManager
{
    // ------------------------------------------------------------
    // Incoming Request / Response ingress
    // (driver → adapter → session → adapter → application)
    //  ^^^^^^^^^^^^^^^^^^^^^^^^^^
    // ------------------------------------------------------------

    /// <summary>
    /// Consumes an incoming request from a remote peer.
    /// </summary>
    internal IncomingRequest ConsumeIncomingRequest(
        uint requestId,
        uint? requestType,
        ReadOnlyMemory<byte> payload)
    {
        // prevent duplicate request ids
        this.RequestContexts.ThrowIfExists(requestId);

        // add a new request context to track the incoming request lifecycle
        var requestContext = RequestContext.CreateIncoming(requestId, requestType, this.Actions, payload);
        this.RequestContexts.Add(requestContext);

        // publish the incoming request to the application
        var incomingRequest = requestContext.GetIncomingRequest();
        this.PublishIncomingRequest(incomingRequest);
        return incomingRequest;
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
    /// Called after protocol-level request semantics have been fully processed.
    /// </remarks>
    internal void PublishIncomingRequest(IncomingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        this.Logger.LogTrace(
            "Publishing incoming request (Id={RequestId})",
            request.RequestId);

        this.Session.IncomingActionSink.PublishIncomingRequest(request);
    }
}
