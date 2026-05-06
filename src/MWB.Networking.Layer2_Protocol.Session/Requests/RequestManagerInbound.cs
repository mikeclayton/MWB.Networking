using MWB.Networking.Layer2_Protocol.Session.Frames;
using MWB.Networking.Layer2_Protocol.Session.Requests.Api;
using MWB.Networking.Layer2_Protocol.Session.Requests.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Session.Requests;

internal sealed class RequestManagerInbound
{
    internal RequestManagerInbound(ProtocolSession session, RequestManager requestManager, RequestEntries requestEntries)
    {
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.RequestManager = requestManager ?? throw new ArgumentNullException(nameof(requestManager));
        this.RequestEntries = requestEntries ?? throw new ArgumentNullException(nameof(requestEntries));
    }

    private ProtocolSession Session
    {
        get;
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
        ProtocolFrame frame,
        uint requestId)
    {
        if (this.RequestEntries.RequestEntryExists(requestId))
        {
            throw ProtocolException.InvalidFrameSequence(
                frame, "Duplicate RequestId");
        }
    }

    private void EnsureInboundRequestExists(
        ProtocolFrame frame,
        uint requestId,
        out RequestEntry requestEntry)
    {
        if (this.RequestEntries.TryGetRequestEntry(requestId, out var result))
        {
            requestEntry = result;
            return;
        }
        throw ProtocolException.InvalidFrameSequence(
            frame, "Unknown or completed RequestId");
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

    internal void ProcessInboundRequestFrame(ProtocolFrame frame)
    {
        this.EnsureFrameHasRequestId(frame, out var requestId);
        this.EnsureInboundRequestDoesNotExist(frame, requestId);

        var requestType = frame.RequestType;

        var requestContext = new RequestContext(requestId, requestType);
        var incomingRequest = new IncomingRequest(this.Session, requestContext);
        var requestEntry = new RequestEntry(requestContext, incomingRequest);

        this.RequestEntries.AddRequestEntry(requestEntry);
        this.Session.OnRequestReceived(incomingRequest, frame.Payload);
    }

    internal void ProcessInboundResponseFrame(ProtocolFrame frame)
    {
        this.EnsureFrameHasRequestId(frame, out var requestId);
        this.EnsureInboundRequestExists(frame, requestId, out var requestEntry);

        // Close the Request based on a terminal frame received from the peer.
        // This MUST NOT emit any protocol frames.
        requestEntry.Context.CloseFromInbound(frame);

        // Tear down all request-scoped streams
        this.Session.StreamManager.TearDownRequestStreams(requestId);

        // Remove the request (terminal)
        this.RequestEntries.RemoveRequestEntry(requestId);
    }
}
