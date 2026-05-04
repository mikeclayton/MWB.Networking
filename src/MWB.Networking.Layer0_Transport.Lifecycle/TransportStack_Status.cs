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
    /// Set by both the provider-initiated and stack-initiated disconnect paths
    /// so that ReadAsync can return EOF (0) after any terminal event, regardless
    /// of which side initiated the disconnect.
    /// </summary>
    private bool _hasTerminated;

    /// <summary>
    /// Volatile flag kept in sync with the Connected lifecycle state so that
    /// <see cref="IsConnected"/> is a single volatile read with no lock acquisition.
    /// Updated inside <see cref="RaiseConnectionStateChanged"/> which already holds _sync.
    /// </summary>
    private volatile bool _isConnected;

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
    public bool IsConnected => _isConnected;

    // -----------------------------
    // Connection state changed events
    // -----------------------------

    private EventHandler<TransportFaultedEventArgs>? _faulted;
    private EventHandler<TransportConnectionState>? _connectionStateChanged;

    /// <summary>
    /// Raised when a fatal transport error occurs.
    /// </summary>
    public event EventHandler<TransportFaultedEventArgs>? Faulted
    {
        add { lock (_sync) { _faulted += value; } }
        remove { lock (_sync) { _faulted -= value; } }
    }

    public event EventHandler<TransportConnectionState>? ConnectionStateChanged
    {
        add { lock (_sync) { _connectionStateChanged += value; } }
        remove { lock (_sync) { _connectionStateChanged -= value; } }
    }

    private void OnConnecting(object? _, EventArgs __) =>
        this.RaiseConnectionStateChanged(TransportConnectionState.Connecting);

    private void OnConnected(object? _, EventArgs __) =>
        this.RaiseConnectionStateChanged(TransportConnectionState.Connected);

    private void OnDisconnecting(object? _, EventArgs __) =>
        this.RaiseConnectionStateChanged(TransportConnectionState.Disconnecting);

    private void OnDisconnected(object? _, TransportDisconnectedEventArgs e)
    {
        this.CleanupForTerminalEvent();
        this.RaiseConnectionStateChanged(TransportConnectionState.Disconnected);
    }

    private void OnFaulted(object? _, TransportFaultedEventArgs e)
    {
        // IMPORTANT:
        // A faulted connection attempt is complete and must
        // not block subsequent ConnectAsync calls.
        this.CleanupForTerminalEvent();

        this.RaiseFaultedEvent(e);
        this.RaiseConnectionStateChanged(TransportConnectionState.Faulted);
    }

    private void RaiseFaultedEvent(TransportFaultedEventArgs e)
    {
        EventHandler<TransportFaultedEventArgs>? handler;
        lock (_sync)
        {
            handler = _faulted;
        }
        handler?.Invoke(this, e);
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
            _isConnected = newState == TransportConnectionState.Connected;
            handler = _connectionStateChanged;
        }

        handler?.Invoke(this, newState);
    }

    // -----------------------------
    // Register / Unregister
    // -----------------------------

    private void RegisterConnectionStatusEvents(
        ObservableConnectionStatus connectionStatus)
    {
        // Reset per-connection state so the first event of the new connection
        // is always raised, even if it matches the terminal state of the
        // previous one (e.g. Fault → reconnect → Fault again).
        _lastRaisedConnectionState = null;
        _hasTerminated = false;
        _isConnected = false;

        connectionStatus.Connecting += this.OnConnecting;
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
