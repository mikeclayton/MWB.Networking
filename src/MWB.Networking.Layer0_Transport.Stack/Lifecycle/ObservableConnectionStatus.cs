using MWB.Networking.Layer0_Transport.Stack.Core.Lifecycle;

namespace MWB.Networking.Layer0_Transport.Stack.Lifecycle;

public sealed class ObservableConnectionStatus : IConnectionStatus
{
    private readonly object _sync = new();

    /// <summary>
    /// The current lifecycle state. Starts at Disconnected to represent
    /// "not yet connected", not a terminal outcome.
    /// </summary>
    private TransportConnectionState _state =
        TransportConnectionState.Disconnected;

    /// <summary>
    /// Indicates whether this lifecycle has reached a terminal outcome
    /// (Disconnected or Faulted). This flag is monotonic: once set, it
    /// never resets.
    ///
    /// We cannot infer terminality from the state enum alone because
    /// Disconnected is both the initial and a terminal state.
    /// </summary>
    private bool _hasTerminated;

    public TransportConnectionState State
    {
        get
        {
            lock (_sync)
            {
                return _state;
            }
        }
    }

    public bool HasTerminated
    {
        get
        {
            lock (_sync)
            {
                return _hasTerminated;
            }
        }
    }

    // -----------------------------
    // Events
    // -----------------------------

    public event EventHandler? Connecting;
    public event EventHandler? Connected;
    public event EventHandler? Disconnecting;
    public event EventHandler<TransportDisconnectedEventArgs>? Disconnected;
    public event EventHandler<TransportFaultedEventArgs>? Faulted;

    // -----------------------------
    // Lifecycle methods
    // -----------------------------

    public void OnConnecting()
    {
        this.Transition(
            expected: TransportConnectionState.Disconnected,
            newState: TransportConnectionState.Connecting,
            onTransition: () => Connecting?.Invoke(this, EventArgs.Empty));
    }

    public void OnConnected()
    {
        this.Transition(
            expected: TransportConnectionState.Connecting,
            newState: TransportConnectionState.Connected,
            onTransition: () => Connected?.Invoke(this, EventArgs.Empty));
    }

    public void OnDisconnecting()
    {
        // Disconnecting is valid from any state where a connection was
        // actively in progress — Connected or Connecting. It is not
        // emitted from the initial Disconnected state (nothing started)
        // or from states that are already terminal or already Disconnecting.
        bool shouldFire;
        lock (_sync)
        {
            shouldFire =
                !_hasTerminated &&
                (_state == TransportConnectionState.Connected ||
                 _state == TransportConnectionState.Connecting);
            if (shouldFire)
            {
                _state = TransportConnectionState.Disconnecting;
            }
        }
        if (shouldFire)
        {
            Disconnecting?.Invoke(this, EventArgs.Empty);
        }
    }

    // ---------- Terminal transitions ----------

    public void OnDisconnected(TransportDisconnectedEventArgs e)
    {
        Terminal(
            TransportConnectionState.Disconnected,
            () => Disconnected?.Invoke(this, e));
    }

    public void OnFaulted(TransportFaultedEventArgs e)
    {
        Terminal(
            TransportConnectionState.Faulted,
            () => Faulted?.Invoke(this, e));
    }

    // -----------------------------
    // Transition helpers
    // -----------------------------

    /// <summary>
    /// Performs a non-terminal state transition (e.g. Connecting, Connected).
    /// Transitions are ignored once a terminal outcome has occurred.
    /// </summary>
    private void Transition(
        TransportConnectionState expected,
        TransportConnectionState newState,
        Action onTransition)
    {
        lock (_sync)
        {
            if (_hasTerminated)
            {
                return;
            }
            if (_state != expected)
            {
                return; // tolerate late / racing signals
            }
            if (_state == newState)
            {
                // to avoid duplicate transition events
                // (shouldn't be reachable, but left in case)
                return;
            }
            _state = newState;
        }
        onTransition();
    }

    /// <summary>
    /// Performs a terminal state transition (Disconnected or Faulted).
    /// Only the first terminal transition is honored.
    /// </summary>
    private void Terminal(
        TransportConnectionState terminal,
        Action onTransition)
    {
        lock (_sync)
        {
            if (_hasTerminated)
            {
                return;
            }
            _hasTerminated = true;
            _state = terminal;
        }
        onTransition();
    }
}
