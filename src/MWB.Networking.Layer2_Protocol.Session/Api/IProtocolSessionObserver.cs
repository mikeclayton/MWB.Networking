using MWB.Networking.Layer2_Protocol.Session.Events.Api;
using MWB.Networking.Layer2_Protocol.Session.Requests.Api;
using MWB.Networking.Layer2_Protocol.Session.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Session.Api;

public interface IProtocolSessionObserver
{
    // === Semantics ===

    event Action<IncomingEvent, ReadOnlyMemory<byte> /* Payload */>? EventReceived;

    event Action<IncomingRequest, ReadOnlyMemory<byte> /* Payload */>? RequestReceived;

    event Action<IncomingStream, StreamMetadata>? StreamOpened;
    event Action<IncomingStream, ReadOnlyMemory<byte> /* Payload */>? StreamDataReceived;
    event Action<IncomingStream, StreamMetadata>? StreamClosed;
    event Action<IncomingStream, StreamMetadata>? StreamAborted;
}
