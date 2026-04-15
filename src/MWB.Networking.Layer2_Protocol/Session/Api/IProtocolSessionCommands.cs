using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Session.Api;

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
