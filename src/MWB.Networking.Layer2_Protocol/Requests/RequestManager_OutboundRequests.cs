using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed partial class RequestManager
{
    // ------------------------------------------------------------
    // Outgoing Request / Response ingress
    // (application → adapter → session → adapter → driver)
    //  ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
    // ------------------------------------------------------------

    private const uint _firstRequestId = 1;
    private uint _nextRequestId = _firstRequestId;

    /// <summary>
    /// Generate a new unique request ID.
    /// </summary>
    private uint GetNextRequestId()
    {
        // get the next value safely (thread-safe, overflow-safe incrementing)
        var next = Interlocked.Increment(ref _nextRequestId);
        if (next == 0)
        {
            // wrapped from uint.MaxValue → 0
            throw new ProtocolException(
                ProtocolErrorKind.InternalError,
                "Request id pool exhausted.");
        }
        return next;
    }

    /// <summary>
    /// Consumes a locally generated outgoing request.
    /// </summary>
    internal OutgoingRequest ConsumeOutgoingRequest(
        uint? requestType,
        ReadOnlyMemory<byte> payload)
    {
        // Generate a new unique request ID
        // (thread-safe, overflow-safe incrementing)
        var requestId = this.GetNextRequestId();

        // add a new request context to track the outgoing request lifecycle
        var requestContext = RequestContext.CreateOutgoing(requestId, requestType, this.Actions, payload);
        this.RequestContexts.Add(requestContext);

        // transmit the protocol request to the remote peer
        var outgoingRequest = requestContext.GetOutgoingRequest();
        this.TransmitOutgoingRequest(outgoingRequest);
        return outgoingRequest;
    }

    // ------------------------------------------------------------
    // Outgoing Request / Response egress
    // (application → adapter → session → adapter → driver)
    //                          ^^^^^^^^^^^^^^^^^^^^^^^^^^
    // ------------------------------------------------------------

    /// <summary>
    /// Transmits an outgoing request for delivery to the remote peer.
    /// </summary>
    internal void TransmitOutgoingRequest(OutgoingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        this.Logger.LogTrace(
            "Transmitting outgoing request (Id={RequestId})",
            request.RequestId);

        this.Session.OutgoingActionSink.TransmitOutgoingRequest(request);
    }
}
