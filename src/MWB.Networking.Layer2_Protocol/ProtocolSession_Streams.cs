using MWB.Networking.Layer2_Protocol.Requests;
using MWB.Networking.Layer2_Protocol.Streams;

namespace MWB.Networking.Layer2_Protocol;

public sealed partial class ProtocolSession : IProtocolSession
{
    // ------------------------------------------------------------------
    // Stream handling - Inbound
    // ------------------------------------------------------------------

    private Dictionary<uint, StreamEntry> StreamEntries
    {
        get;
    } = [];

    private void RemoveStream(uint streamId)
    {
        this.StreamEntries.Remove(streamId);
    }
}
