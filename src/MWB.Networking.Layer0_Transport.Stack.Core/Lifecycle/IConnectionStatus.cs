namespace MWB.Networking.Layer0_Transport.Stack.Core.Lifecycle;

public interface IConnectionStatus
{
    TransportConnectionState State
    {
        get;
    }

    event EventHandler Connecting;
    event EventHandler Connected;
    event EventHandler Disconnecting;
    event EventHandler<TransportDisconnectedEventArgs> Disconnected;
    event EventHandler<TransportFaultedEventArgs> Faulted;

    void OnConnecting();
    void OnConnected();
    void OnDisconnecting();
    void OnDisconnected(TransportDisconnectedEventArgs e);
    void OnFaulted(TransportFaultedEventArgs e);
}
