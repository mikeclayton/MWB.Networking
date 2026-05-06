namespace MWB.Networking.Layer0_Transport.Driver.Abstractions;

public interface ITransportConnectionEvents
{
    void OnConnectionClosed();

    void OnConnectionFaulted(Exception error);
}
