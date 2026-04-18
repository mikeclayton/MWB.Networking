namespace MWB.Networking.Layer2_Protocol.Session.Api;

public interface IProtocolSessionLifecycle
{
    Task Ready
    {
        get;
    }

    Task StartAsync(CancellationToken ct);
}
