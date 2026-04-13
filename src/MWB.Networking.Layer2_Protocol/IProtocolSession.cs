using MWB.Networking.Layer2_Protocol.Requests;
using MWB.Networking.Layer2_Protocol.Streams;

namespace MWB.Networking.Layer2_Protocol;

public interface IProtocolSession
{
    // === Semantics ===

    event Action<uint /* EventType */, ReadOnlyMemory<byte> /* Payload */>? EventReceived;

    event Action<IncomingRequest, ReadOnlyMemory<byte> /* Payload */>? RequestReceived;

    event Action<IncomingStream, StreamMetadata>? StreamOpened;
    event Action<IncomingStream, ReadOnlyMemory<byte> /* Payload */>? StreamDataReceived;
    event Action<IncomingStream>? StreamClosed;

    //// === Introspection===

    /// <summary>
    /// Returns a snapshot of protocol state for tests and diagnostics.
    /// </summary>
    ProtocolSnapshot Snapshot();
}
