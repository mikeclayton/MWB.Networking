namespace MWB.Networking.Layer2_Protocol.Session.Api;

public interface IProtocolSessionLifecycle
{
    Task WhenReady
    {
        get;
    }

    Task StartAsync(CancellationToken ct);
}
