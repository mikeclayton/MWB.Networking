using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests;

namespace MWB.Networking.Layer2_Protocol;

public sealed partial class ProtocolSession : IProtocolSession
{
    // ------------------------------------------------------------------
    // Request handling
    // ------------------------------------------------------------------

    /// <summary>
    /// Raised when a new protocol request is received from the peer.
    /// </summary>
    public event Action<IncomingRequest, ReadOnlyMemory<byte> /* Payload */>? RequestReceived;

    private Dictionary<uint, RequestContext> RequestContexts
    {
        get;
    } = [];

    private uint NextRequestId
    {
        get;
        set;
    } = 1;

    public void SendRequest(ReadOnlyMemory<byte> payload)
    {
        // Generate a new unique request ID<br>
        var requestId = this.NextRequestId++;
        // Create and track request context
        var context = new RequestContext(requestId);
        this.RequestContexts.Add(requestId, context);
        // Emit the protocol request frame to the peer
        this.EnqueueOutboundFrame(ProtocolFrames.Request(requestId, payload));
    }

    private void RaiseRequestReceived(IncomingRequest request, ReadOnlyMemory<byte> payload)
        => this.RequestReceived?.Invoke(request, payload);

    private void ProcessNewRequestFrame(ProtocolFrame frame)
    {
        if (frame.RequestId is null)
        {
            throw ProtocolError(frame, "Request frame missing RequestId");
        }

        var requestId = frame.RequestId.Value;

        if (this.RequestContexts.ContainsKey(requestId))
        {
            throw ProtocolError(frame, "Duplicate RequestId");
        }

        var context = new RequestContext(requestId);
        this.RequestContexts.Add(requestId, context);

        // Application-facing request handle
        var request = new IncomingRequest(
            requestSink: this,
            requestId: requestId);

        this.RaiseRequestReceived(request, frame.Payload);
    }

    private void ProcessRequestFrame(ProtocolFrame frame)
    {
        if (frame.RequestId is null)
        {
            throw ProtocolError(frame, "Request-related frame missing RequestId");
        }

        if (!this.RequestContexts.TryGetValue(frame.RequestId.Value, out var ctx))
        {
            throw ProtocolError(frame, "Unknown RequestId");
        }

        ctx.ProcessFrame(frame, this.EnqueueOutboundFrame, this.RemoveRequest);
    }

    private void RemoveRequest(uint requestId)
    {
        this.RequestContexts.Remove(requestId);
    }
}
