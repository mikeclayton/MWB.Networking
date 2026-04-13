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

    private static bool IsTerminalRequestFrame(ProtocolFrame frame)
    {
        return frame.Kind == ProtocolFrameKind.Response
            || frame.Kind == ProtocolFrameKind.Error;
    }

    private void RaiseRequestReceived(IncomingRequest request, ReadOnlyMemory<byte> payload)
        => this.RequestReceived?.Invoke(request, payload);

    private void RemoveRequest(uint requestId)
    {
        // auto-close streams owned by the request
        var snapshot = this.StreamEntries.ToList();
        foreach (var (streamId, entry) in snapshot)
        {
            if (entry.Context.OwningRequest?.RequestId == requestId)
            {
                entry.Context.Close();
                this.RemoveStream(streamId);
            }
        }
        this.RequestContexts.Remove(requestId);
    }
}
