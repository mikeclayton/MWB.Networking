using MWB.Networking.Layer0_Transport.Lifecycle.Stack;

public sealed class ObservableConnectionStatus
{
    private readonly object _sync = new();

    private TransportConnectionState _state =
        TransportConnectionState.Disconnected;

    public TransportConnectionState State
    {
        get { lock (_sync) return _state; }
    }

    public event EventHandler? Connecting;
    public event EventHandler? Connected;
    public event EventHandler? Disconnecting;
    public event EventHandler<TransportDisconnectedEventArgs>? Disconnected;
    public event EventHandler<TransportFaultedEventArgs>? Faulted;

    // ---------- Observation API ----------

    public void OnConnecting()
    {
        Transition(
            expected: TransportConnectionState.Disconnected,
            next: TransportConnectionState.Connecting,
            onTransition: () => Connecting?.Invoke(this, EventArgs.Empty));
    }

    public void OnConnected()
    {
        Transition(
            expected: TransportConnectionState.Connecting,
            next: TransportConnectionState.Connected,
            onTransition: () => Connected?.Invoke(this, EventArgs.Empty));
    }

    public void OnDisconnecting()
    {
        Transition(
            expected: TransportConnectionState.Connected,
            next: TransportConnectionState.Disconnecting,
            onTransition: () => Disconnecting?.Invoke(this, EventArgs.Empty));
    }

    // ---------- Terminal transitions ----------

    public void OnDisconnected(TransportDisconnectedEventArgs e)
    {
        TransitionToTerminal(
            TransportConnectionState.Disconnected,
            () => Disconnected?.Invoke(this, e));
    }

    public void OnFaulted(TransportFaultedEventArgs e)
    {
        TransitionToTerminal(
            TransportConnectionState.Faulted,
            () => Faulted?.Invoke(this, e));
    }

    // ---------- Helpers ----------

    private void Transition(
        TransportConnectionState expected,
        TransportConnectionState next,
        Action onTransition)
    {
        lock (_sync)
        {
            if (IsTerminal(_state))
                return;

            if (_state == next)
                return;

            if (_state != expected)
                return; // tolerate late / racing signals

            _state = next;
        }

        onTransition();
    }

    private void TransitionToTerminal(
        TransportConnectionState terminal,
        Action onTransition)
    {
        lock (_sync)
        {
            if (IsTerminal(_state))
                return;

            _state = terminal;
        }

        onTransition();
    }

    private static bool IsTerminal(TransportConnectionState state) =>
        state == TransportConnectionState.Disconnected ||
        state == TransportConnectionState.Faulted;
}
