namespace MWB.Networking.Layer2_Protocol.Session.Api;

public interface IProtocolSessionLifecycle
{
    //void AttachProtocolDriver(ProtocolDriver driver);

    Task StartAsync(CancellationToken ct);

    Task WhenReady
    {
        get;
    }
}
