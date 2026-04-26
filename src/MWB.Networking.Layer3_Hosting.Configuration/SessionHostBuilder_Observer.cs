using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer3_Hosting.Configuration;

public sealed partial class SessionHostBuilder
{

    private readonly ProtocolSessionObserverConfiguration _observerConfig = new();

    //public ProtocolSessionBuilder ConfigureObservers(
    //    Action<ProtocolSessionObserverConfiguration> configure)
    //{
    //    ArgumentNullException.ThrowIfNull(configure);
    //    configure(_observerConfig);
    //    return this;
    //}
    public SessionHostBuilder OnEventReceived(
        Action<uint, ReadOnlyMemory<byte>>? handler)
    {
        _observerConfig.EventReceived = handler;
        return this;
    }

    public SessionHostBuilder OnRequestReceived(
        Action<IncomingRequest, ReadOnlyMemory<byte>>? handler)
    {
        _observerConfig.RequestReceived = handler;
        return this;
    }

    public SessionHostBuilder OnStreamOpened(
        Action<IncomingStream, StreamMetadata>? handler)
    {
        _observerConfig.StreamOpened = handler;
        return this;
    }

    public SessionHostBuilder OnStreamData(
        Action<IncomingStream, ReadOnlyMemory<byte>>? handler)
    {
        _observerConfig.StreamDataReceived = handler;
        return this;
    }

    public SessionHostBuilder OnStreamClosed(
        Action<IncomingStream, StreamMetadata>? handler)
    {
        _observerConfig.StreamClosed = handler;
        return this;
    }
}
