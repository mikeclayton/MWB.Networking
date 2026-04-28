using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Lifecycle.Api;

namespace MWB.Networking.Layer2_Protocol.Session.Api;

public interface IProtocolSessionCommands
{
    // ------------------------------------------------------------
    // Events
    // ------------------------------------------------------------

    void SendEvent(ReadOnlyMemory<byte> payload = default)
    {
        this.SendEvent(null, payload);
    }

    void SendEvent(uint? eventType, ReadOnlyMemory<byte> payload = default);

    // ------------------------------------------------------------
    // Requests
    // ------------------------------------------------------------

    OutgoingRequest SendRequest(ReadOnlyMemory<byte> payload = default)
    {
        return this.SendRequest(null, payload);
    }

    OutgoingRequest SendRequest(uint? requestType, ReadOnlyMemory<byte> payload = default);

    // ------------------------------------------------------------------
    // Streams
    // ------------------------------------------------------------------

    OutgoingStream OpenSessionStream(ReadOnlyMemory<byte> metadata = default)
    {
        return this.OpenSessionStream(null, metadata);
    }

    OutgoingStream OpenSessionStream(uint? streamType, ReadOnlyMemory<byte> metadata = default);
}
