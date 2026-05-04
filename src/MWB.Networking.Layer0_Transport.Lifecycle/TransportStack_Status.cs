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
    {
        get
        {
            lock (_sync)
            {
                return _state == StackState.Connected;
            }
        }
    }

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

    private void OnConnecting(object? _, EventArgs __)
    {
        lock (_sync)
        {
            // Only meaningful when starting a new connection attempt
            if (_state != StackState.Connecting)
                return;
        }

        this.RaiseConnectionStateChanged(TransportConnectionState.Connecting);
    }

    private void OnConnected(object? _, EventArgs __)
    {
        lock (_sync)
        {
            // Only a connecting stack can become connected.
            // Ignore stray / late signals.
            if (_state != StackState.Connecting)
            {
                return;
            }

            _state = StackState.Connected;

            // Record that we have successfully established
            // a logical connection at least once.
            _hasEverConnected = true;
        }
        this.RaiseConnectionStateChanged(TransportConnectionState.Connected);
    }

    private void OnDisconnecting(object? _, EventArgs __)
    {
        lock (_sync)
        {
            // Disconnecting is valid exactly once during an active attempt
            if (_state != StackState.Connected &&
                _state != StackState.Connecting)
            {
                return;
            }

            _state = StackState.Disconnecting;
        }
        this.RaiseConnectionStateChanged(TransportConnectionState.Disconnecting);
    }

    private void OnDisconnected(object? _, TransportDisconnectedEventArgs e)
    {
        bool shouldRaise;

        lock (_sync)
        {
            // Terminal events are idempotent
            if (_state == StackState.Terminated)
            {
                return;
            }

            shouldRaise =
                _state == StackState.Connected ||
                _state == StackState.Connecting ||
                _state == StackState.Disconnecting;

            // IMPORTANT:
            // Immediately leave Connected so IsConnected becomes false
            _state = StackState.Terminated;
        }

        // CleanupForTerminalEvent transitions state → Terminated
        this.CleanupForTerminalEvent();

        if (shouldRaise)
        {
            this.RaiseConnectionStateChanged(TransportConnectionState.Disconnected);
        }

        // Provider disconnects are recoverable → reset to Idle
        lock (_sync)
        {
            _state = StackState.Idle;
        }
    }

    private void OnFaulted(object? _, TransportFaultedEventArgs e)
    {
        bool shouldRaise;

        lock (_sync)
        {
            if (_state == StackState.Terminated)
            {
                return;
            }

            shouldRaise =
                _state == StackState.Connecting ||
                _state == StackState.Connected ||
                _state == StackState.Disconnecting;

            // IMPORTANT:
            // Immediately leave Connected/Connecting so IsConnected becomes false
            _state = StackState.Terminated;
        }

        // A faulted connection attempt is complete and must
        // not block subsequent ConnectAsync calls.
        this.CleanupForTerminalEvent();

        this.RaiseFaultedEvent(e);
        if (shouldRaise)
        {
            this.RaiseConnectionStateChanged(TransportConnectionState.Faulted);
        }

        // Provider faults are recoverable → reset to Idle
        lock (_sync)
        {
            _state = StackState.Idle;
        }
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
