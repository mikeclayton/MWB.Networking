using MWB.Networking.Layer0_Transport.Lifecycle.Stack;

namespace MWB.Networking.Layer0_Transport.Lifecycle;

public sealed partial class TransportStack
{
    // -----------------------------
    // Public state
    // -----------------------------

    /// <summary>
    /// Tracks the last emitted connection state in case a connection
    /// sends multiple identical states in succession. This can be used
    /// to avoid raising duplicate events
    /// </summary>
    private TransportConnectionState? _lastRaisedConnectionState;

    /// <summary>
    /// Gets the current connection status.
    /// </summary>
    private ObservableConnectionStatus? ConnectionStatus
    {
        get;
        set;
    }

    public TransportConnectionState? ConnectionState
    {
        get
        {
            lock (_sync)
            {
                return this.ConnectionStatus?.State;
            }
        }
    }

    /// <summary>
    /// True if the stack is currently connected.
    /// </summary>
    public bool IsConnected
        => this.ConnectionState == TransportConnectionState.Connected;

    // -----------------------------
    // Connection state changed events
    // -----------------------------

    /// <summary>
    /// Raised when a fatal transport error occurs.
    /// </summary>
    public event EventHandler<TransportFaultedEventArgs>? Faulted;

    public event EventHandler<TransportConnectionState>? ConnectionStateChanged;

    private void OnConnecting(object? _, EventArgs __) =>
        this.RaiseConnectionStateChanged(TransportConnectionState.Connecting);

    private void OnConnected(object? _, EventArgs __) =>
        this.RaiseConnectionStateChanged(TransportConnectionState.Connected);

    private void OnDisconnecting(object? _, EventArgs __) =>
        this.RaiseConnectionStateChanged(TransportConnectionState.Disconnecting);

    private void OnDisconnected(object? _, TransportDisconnectedEventArgs e)
    {
        this.CleanupConnection();
        this.CleanupConnectionOnDisconnect();
        this.RaiseConnectionStateChanged(TransportConnectionState.Disconnected);
    }

    private void OnFaulted(object? _, TransportFaultedEventArgs e)
    {
        this.CleanupConnection();
        this.RaiseFaultedEvent(e);
        this.RaiseConnectionStateChanged(TransportConnectionState.Faulted);
    }

    private void RaiseConnectionStateChanged(TransportConnectionState newState)
    {
        EventHandler<TransportConnectionState>? handler = null;

        lock (_sync)
        {
            if (_lastRaisedConnectionState == newState)
            {
                return;
            }

            _lastRaisedConnectionState = newState;
            handler = this.ConnectionStateChanged;
        }

        handler?.Invoke(this, newState);
    }

    // -----------------------------
    // Register / Unregister
    // -----------------------------

    private void RegisterConnectionStatusEvents(
        ObservableConnectionStatus connectionStatus)
    {
        connectionStatus.Connecting += OnConnecting;
        connectionStatus.Connected += this.OnConnected;
        connectionStatus.Disconnecting += this.OnDisconnecting;
        connectionStatus.Disconnected += this.OnDisconnected;
        connectionStatus.Faulted += this.OnFaulted;
    }

    private void UnregisterConnectionStatusEvents(
        ObservableConnectionStatus connectionStatus)
    {
        connectionStatus.Connecting -= this.OnConnecting;
        connectionStatus.Connected -= this.OnConnected;
        connectionStatus.Disconnecting -= this.OnDisconnecting;
        connectionStatus.Disconnected -= this.OnDisconnected;
        connectionStatus.Faulted -= this.OnFaulted;
    }
}
