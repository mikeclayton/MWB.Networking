using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Session;

internal sealed partial class ProtocolSession : IProtocolSessionObserver
{
    private IProtocolSessionObserver AsObserver()
        => this;

    // ------------------------------------------------------------------
    // Event handling
    // ------------------------------------------------------------------

    public event Action<uint /* EventType */, ReadOnlyMemory<byte> /* Payload */>? EventReceived;

    internal void OnEventReceived(uint eventType, ReadOnlyMemory<byte> payload)
        => this.EventReceived?.Invoke(eventType, payload);

    // ------------------------------------------------------------------
    // Requests
    // ------------------------------------------------------------------

    /// <summary>
    /// Raised when a new protocol request is received from the peer.
    /// </summary>
    public event Action<IncomingRequest, ReadOnlyMemory<byte> /* Payload */>? RequestReceived;

    internal void OnRequestReceived(IncomingRequest request, ReadOnlyMemory<byte> payload)
    {
        this.RequestReceived?.Invoke(request, payload);
    }

    // ------------------------------------------------------------------
    // Streams
    // ------------------------------------------------------------------

    public event Action<IncomingStream, StreamMetadata>? StreamOpened;

    public event Action<IncomingStream, ReadOnlyMemory<byte> /* Payload */>? StreamDataReceived;

    public event Action<IncomingStream>? StreamClosed;

    internal void OnStreamOpened(IncomingStream stream, StreamMetadata metadata)
        => this.StreamOpened?.Invoke(stream, metadata);

    internal void OnStreamDataReceived(IncomingStream stream, ReadOnlyMemory<byte> payload)
        => this.StreamDataReceived?.Invoke(stream, payload);

    internal void OnStreamClosed(IncomingStream stream)
        => this.StreamClosed?.Invoke(stream);

    public OutgoingStream OpenSessionStream(ReadOnlyMemory<byte> metadata)
    {
        return this.StreamManager.OpenSessionStream(metadata: metadata);
    }
}
