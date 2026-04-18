using MWB.Networking.Layer2_Protocol.Session.Api;

namespace MWB.Networking.Hosting;

public sealed partial class ProtocolSessionBuilder
{

    private readonly ProtocolSessionObserverConfiguration _observerConfig = new();

    public ProtocolSessionBuilder ConfigureObservers(
        Action<ProtocolSessionObserverConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_observerConfig);
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
