using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Streams.Api;
using MWB.Networking.Logging;

namespace MWB.Networking.Layer2_Protocol.Session;

internal sealed partial class ProtocolSession : IProtocolSessionCommands
{
    private IProtocolSessionCommands AsCommands()
        => this;

    // ------------------------------------------------------------
    // Events
    // ------------------------------------------------------------

    [LogMethod]
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
}
