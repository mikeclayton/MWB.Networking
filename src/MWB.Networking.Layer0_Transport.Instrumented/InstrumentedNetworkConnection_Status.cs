using MWB.Networking.Layer0_Transport.Lifecycle.Stack;

namespace MWB.Networking.Layer0_Transport.Instrumented;

public sealed partial class InstrumentedNetworkConnection
{
    private readonly ObservableConnectionStatus _connectionStatus;

    // --- Lifecycle control (test-facing API) ----------------

    public void SignalConnecting()
        => _connectionStatus.OnConnecting();

    public void SignalConnected()
        => _connectionStatus.OnConnected();

    public void SignalDisconnecting()
        => _connectionStatus.OnDisconnecting();

    public void SignalDisconnected(string reason)
        => _connectionStatus.OnDisconnected(
            new TransportDisconnectedEventArgs(reason));

    public void SignalFaulted(string reason, Exception? exception = null)
        => _connectionStatus.OnFaulted(
            new TransportFaultedEventArgs(reason, exception));
}
