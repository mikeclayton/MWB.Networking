using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Hosting;

public sealed partial class ProtocolSessionBuilder
{

    private readonly ProtocolSessionObserverConfiguration _observerConfig = new();

    //public ProtocolSessionBuilder ConfigureObservers(
    //    Action<ProtocolSessionObserverConfiguration> configure)
    //{
    //    ArgumentNullException.ThrowIfNull(configure);
    //    configure(_observerConfig);
    //    return this;
    //}
    public ProtocolSessionBuilder OnEventReceived(
        Action<uint, ReadOnlyMemory<byte>>? handler)
    {
        _observerConfig.EventReceived = handler;
        return this;
    }

    public ProtocolSessionBuilder OnRequestReceived(
        Action<IncomingRequest, ReadOnlyMemory<byte>>? handler)
    {
        _observerConfig.RequestReceived = handler;
        return this;
    }

    public ProtocolSessionBuilder OnStreamOpened(
        Action<IncomingStream, StreamMetadata>? handler)
    {
        _observerConfig.StreamOpened = handler;
        return this;
    }

    public ProtocolSessionBuilder OnStreamData(
        Action<IncomingStream, ReadOnlyMemory<byte>>? handler)
    {
        _observerConfig.StreamDataReceived = handler;
        return this;
    }

    public ProtocolSessionBuilder OnStreamClosed(
        Action<IncomingStream, StreamMetadata>? handler)
    {
        _observerConfig.StreamClosed = handler;
        return this;
    }

    private static void AssignObservers(
        ProtocolSessionHandle session,
        ProtocolSessionObserverConfiguration config)
    {
        var observer = session.Observer;

        if (config.EventReceived is not null)
        {
            observer.EventReceived += config.EventReceived;
        }

        if (config.RequestReceived is not null)
        {
            observer.RequestReceived += config.RequestReceived;
        }

        if (config.StreamOpened is not null)
        {
            observer.StreamOpened += config.StreamOpened;
        }

        if (config.StreamDataReceived is not null)
        {
            observer.StreamDataReceived += config.StreamDataReceived;
        }

        if (config.StreamClosed is not null)
        {
            observer.StreamClosed += config.StreamClosed;
        }
    }
}
