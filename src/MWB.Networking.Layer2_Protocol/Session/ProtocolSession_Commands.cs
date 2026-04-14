using MWB.Networking.Layer2_Protocol.Requests;
using MWB.Networking.Layer2_Protocol.Streams;

namespace MWB.Networking.Layer2_Protocol.Session;

public sealed partial class ProtocolSession : IProtocolSessionCommands
{
    // ------------------------------------------------------------
    // Events
    // ------------------------------------------------------------

    void IProtocolSessionCommands.SendEvent(uint eventType, ReadOnlyMemory<byte> payload)
    {
        this.EventManager.SendEvent(eventType, payload);
    }

    // ------------------------------------------------------------
    // Requests
    // ------------------------------------------------------------

    OutgoingRequest IProtocolSessionCommands.SendRequest(ReadOnlyMemory<byte> payload)
    {
        return this.RequestManager.SendRequest(payload);
    }

    // ------------------------------------------------------------------
    // Streams
    // ------------------------------------------------------------------

    OutgoingStream IProtocolSessionCommands.OpenSessionStream(ReadOnlyMemory<byte> metadata)
    {
        return this.StreamManager.OpenSessionStream(metadata);
    }

    // ------------------------------------------------------------------
    // Snapshots
    // ------------------------------------------------------------------
}
