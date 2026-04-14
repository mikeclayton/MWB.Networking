using MWB.Networking.Layer2_Protocol.Requests;
using MWB.Networking.Layer2_Protocol.Streams;

namespace MWB.Networking.Layer2_Protocol.Session;

public interface IProtocolSessionCommands
{
    // ------------------------------------------------------------
    // Events
    // ------------------------------------------------------------

    void SendEvent(uint eventType, ReadOnlyMemory<byte> payload);

    // ------------------------------------------------------------
    // Requests
    // ------------------------------------------------------------

    OutgoingRequest SendRequest(ReadOnlyMemory<byte> payload);

    // ------------------------------------------------------------------
    // Streams
    // ------------------------------------------------------------------

    OutgoingStream OpenSessionStream(ReadOnlyMemory<byte> metadata);
}
