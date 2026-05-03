using MWB.Networking.Layer2_Protocol.Events.Api;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Lifecycle.Api;

namespace MWB.Networking.Layer2_Protocol.Session.Api;

public interface IProtocolSessionObserver
{
    // === Semantics ===

    event Action<IncomingEvent, ReadOnlyMemory<byte> /* Payload */>? EventReceived;

    event Action<IncomingRequest, ReadOnlyMemory<byte> /* Payload */>? RequestReceived;

    event Action<IncomingStream, StreamMetadata>? StreamOpened;
    event Action<IncomingStream, ReadOnlyMemory<byte> /* Payload */>? StreamDataReceived;
    event Action<IncomingStream, StreamMetadata>? StreamClosed;
}
