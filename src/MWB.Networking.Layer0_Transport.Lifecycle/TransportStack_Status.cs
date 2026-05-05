using MWB.Networking.Layer0_Transport.Lifecycle.Fsm;
using MWB.Networking.Layer0_Transport.Lifecycle.Stack;
using MWB.Networking.Logging;

namespace MWB.Networking.Layer0_Transport.Lifecycle;

public sealed partial class TransportStack
{
    // -----------------------------
    // Public state
    // -----------------------------

    private bool _hasEverConnected = false;
      
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
                return _machine.State == TransportStackState.Connected;
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
        using var loggerScope = this.Logger.BeginMethodLoggingScope(this);
        TransportStackTransition transition;
        lock (_sync)
        {
            transition = _machine.Process(
                TransportStackInputKind.ProviderConnecting);
        }
        this.Apply(transition);
    }


    private void OnConnected(object? _, EventArgs __)
    {
        using var loggerScope = this.Logger.BeginMethodLoggingScope(this);
        TransportStackTransition transition;
        lock (_sync)
        {
            // History flag remains orthogonal to lifecycle
            _hasEverConnected = true;

            transition = _machine.Process(
                TransportStackInputKind.ProviderConnected);
        }
        this.Apply(transition);
    }

    private void OnDisconnecting(object? _, EventArgs __)
    {
        using var loggerScope = this.Logger.BeginMethodLoggingScope(this);
        TransportStackTransition transition;
        lock (_sync)
        {
            transition = _machine.Process(
                TransportStackInputKind.DisconnectRequested);
        }
        this.Apply(transition);
    }

    private void OnDisconnected(object? _, TransportDisconnectedEventArgs e)
    {
        using var loggerScope = this.Logger.BeginMethodLoggingScope(this);
        TransportStackTransition transition;
        lock (_sync)
        {
            transition = _machine.Process(
                TransportStackInputKind.ProviderDisconnected);
        }
        this.Apply(transition);
    }

    private void OnFaulted(object? _, TransportFaultedEventArgs e)
    {
        using var loggerScope = this.Logger.BeginMethodLoggingScope(this);
        TransportStackTransition transition;
        lock (_sync)
        {
            transition = _machine.Process(
                TransportStackInputKind.ProviderFaulted, e);
        }
        this.Apply(transition);
    }

    private void RaiseFaultedEvent(TransportFaultedEventArgs e)
    {
        using var loggerScope = this.Logger.BeginMethodLoggingScope(this);
        EventHandler<TransportFaultedEventArgs>? handler;
        lock (_sync)
        {
            handler = _faulted;
        }
        handler?.Invoke(this, e);
    }

    private void RaiseConnectionStateChanged(TransportConnectionState newState)
    {
        using var loggerScope = this.Logger.BeginMethodLoggingScope(this);
        EventHandler<TransportConnectionState>? handler;
        lock (_sync)
        {
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
        using var loggerScope = this.Logger.BeginMethodLoggingScope(this);
        connectionStatus.Connecting += this.OnConnecting;
        connectionStatus.Connected += this.OnConnected;
        connectionStatus.Disconnecting += this.OnDisconnecting;
        connectionStatus.Disconnected += this.OnDisconnected;
        connectionStatus.Faulted += this.OnFaulted;
    }

    private void UnregisterConnectionStatusEvents(
        ObservableConnectionStatus connectionStatus)
    {
        using var loggerScope = this.Logger.BeginMethodLoggingScope(this);
        connectionStatus.Connecting -= this.OnConnecting;
        connectionStatus.Connected -= this.OnConnected;
        connectionStatus.Disconnecting -= this.OnDisconnecting;
        connectionStatus.Disconnected -= this.OnDisconnected;
        connectionStatus.Faulted -= this.OnFaulted;
    }
}
