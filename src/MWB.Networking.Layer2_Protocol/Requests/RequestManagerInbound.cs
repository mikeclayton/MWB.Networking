using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed class RequestManagerInbound
{
    internal RequestManagerInbound(
        RequestManager requestManager,
        RequestEntries requestEntries)
    {
        this.RequestManager = requestManager ?? throw new ArgumentNullException(nameof(requestManager));
        this.RequestEntries = requestEntries ?? throw new ArgumentNullException(nameof(requestEntries));
    }

    private RequestManager RequestManager
    {
        get;
    }

    private RequestEntries RequestEntries
    {
        get;
    }

    // ------------------------------------------------------------------
    // Utility methods
    // ------------------------------------------------------------------

    private void EnsureFrameHasRequestId(
        ProtocolFrame frame,
        out uint requestId)
    {
        requestId = frame.RequestId
            ?? throw ProtocolException.ProtocolViolation(
                frame,
                $"{nameof(ProtocolFrame)} with {nameof(ProtocolFrame.Kind)} of {nameof(ProtocolFrameKind)} must have a {nameof(ProtocolFrame.RequestId)}");
    }

    private void EnsureInboundRequestDoesNotExist(
        uint requestId)
    {
        if (this.RequestEntries.RequestEntryExists(requestId))
        {
            throw ProtocolException.InvalidSequence(
                $"Duplicate RequestId {requestId}");
        }
    }

    private RequestEntry EnsureInboundRequestExists(
        uint requestId)
    {
        if (this.RequestEntries.TryGetRequestEntry(requestId, out var result))
        {
            return result;
        }
        throw ProtocolException.InvalidSequence(
            $"Unknown or completed RequestId {requestId}");
    }

    // ------------------------------------------------------------------
    // Incoming Request wrappers
    // ------------------------------------------------------------------

    internal void RemoveRequestEntry(RequestEntry entry)
    {
        this.RequestEntries.RemoveRequestEntry(entry.RequestId);
    }

    // ------------------------------------------------------------------
    // Request handling - Inbound
    // ------------------------------------------------------------------

    internal void ProcessInboundRequest(
        uint requestId,
        uint? requestType,
        ReadOnlyMemory<byte> payload)
    {
        this.EnsureInboundRequestDoesNotExist(requestId);

        var requestContext = new RequestContext(requestId, requestType);
        var incomingRequest = new IncomingRequest(this.RequestManager, requestContext);
        var requestEntry = new RequestEntry(requestContext, incomingRequest);

        this.RequestEntries.AddRequestEntry(requestEntry);
        this.RequestManager.PublishIncomingRequest(incomingRequest, payload);
    }

    internal void ProcessInboundResponse(IncomingResponse response)
    {
        var requestEntry = this.EnsureInboundRequestExists(response.RequestId);

        // A Response or Error frame must only close a request that *we* initiated
        // (an outgoing request). If the ID matches a request the peer opened (an
        // incoming entry), the frame is a protocol violation: the peer is trying to
        // respond to their own request, which makes no sense.
        if (!requestEntry.IsOutgoing)
        {
            throw ProtocolException.InvalidSequence(
                "Response or Error frame targets an incoming request, not an outgoing one.");
        }

        // complete the awaiting caller
        requestEntry.Context.CloseFromInbound(response);

        // Tear down all request-scoped streams
        this.RequestManager.Session.StreamManager.TearDownRequestStreams(response.RequestId);

        // Remove the request (terminal)
        this.RequestEntries.RemoveRequestEntry(response.RequestId);
    }
}
