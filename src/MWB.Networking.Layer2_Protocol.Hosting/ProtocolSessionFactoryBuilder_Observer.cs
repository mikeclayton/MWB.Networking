using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Hosting;

public sealed partial class ProtocolSessionFactoryBuilder
{

    private readonly ProtocolSessionObserverConfiguration _observerConfig = new();

    public ProtocolSessionFactoryBuilder OnEventReceived(
        Action<uint, ReadOnlyMemory<byte>>? handler)
    {
        _observerConfig.EventReceived = handler;
        return this;
    }

    public ProtocolSessionFactoryBuilder OnRequestReceived(
        Action<IncomingRequest, ReadOnlyMemory<byte>>? handler)
    {
        _observerConfig.RequestReceived = handler;
        return this;
    }

    public ProtocolSessionFactoryBuilder OnStreamOpened(
        Action<IncomingStream, StreamMetadata>? handler)
    {
        _observerConfig.StreamOpened = handler;
        return this;
    }

    public ProtocolSessionFactoryBuilder OnStreamData(
        Action<IncomingStream, ReadOnlyMemory<byte>>? handler)
    {
        _observerConfig.StreamDataReceived = handler;
        return this;
    }

    public ProtocolSessionFactoryBuilder OnStreamClosed(
        Action<IncomingStream, StreamMetadata>? handler)
    {
        _observerConfig.StreamClosed = handler;
        return this;
    }
}
