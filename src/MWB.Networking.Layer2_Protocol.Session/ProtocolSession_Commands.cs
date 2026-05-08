using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Session.Requests.Api;
using MWB.Networking.Layer2_Protocol.Session.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Session;

sealed partial class ProtocolSession : IProtocolSessionCommands
{
    private IProtocolSessionCommands AsCommands()
        => this;

    // ------------------------------------------------------------
    // Events
    // ------------------------------------------------------------

    void IProtocolSessionCommands.SendEvent(uint? eventType, ReadOnlyMemory<byte> payload)
    {
        this.EventManager.SendOutboundEvent(eventType, payload);
    }

    // ------------------------------------------------------------
    // Requests
    // ------------------------------------------------------------

    OutgoingRequest IProtocolSessionCommands.SendRequest(uint? requestType, ReadOnlyMemory<byte> payload)
    {
        return this.RequestManager.Outbound.SendRequest(requestType, payload);
    }

    // ------------------------------------------------------------------
    // Streams
    // ------------------------------------------------------------------

    OutgoingStream IProtocolSessionCommands.OpenSessionStream(uint? streamType, ReadOnlyMemory<byte> metadata)
    {
        return this.StreamManager.Outbound.OpenSessionStream(streamType, metadata);
    }
}
