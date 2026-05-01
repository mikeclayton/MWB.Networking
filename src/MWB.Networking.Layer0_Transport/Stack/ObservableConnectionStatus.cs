namespace MWB.Networking.Layer0_Transport.Stack;

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
    public event EventHandler? Disconnected;
    public event EventHandler<TransportFaultedEventArgs>? Faulted;

    // ---------- Observation API ----------

    public void OnConnecting()
    {
        Transition(
            expected: TransportConnectionState.Disconnected,
            next: TransportConnectionState.Connecting,
            onTransition: () => this.Connecting?.Invoke(this, EventArgs.Empty));
    }

    public void OnConnected()
    {
        Transition(
            expected: TransportConnectionState.Connecting,
            next: TransportConnectionState.Connected,
            onTransition: () => this.Connected?.Invoke(this, EventArgs.Empty));
    }

    public void OnDisconnecting()
    {
        Transition(
            expected: TransportConnectionState.Connected,
            next: TransportConnectionState.Disconnecting,
            onTransition: () => this.Disconnecting?.Invoke(this, EventArgs.Empty));
    }

    public void OnDisconnected()
    {
        TransitionAnyOf(
            allowedFrom:
            [
                TransportConnectionState.Connecting,
                TransportConnectionState.Connected,
                TransportConnectionState.Disconnecting,
                TransportConnectionState.Faulted
            ],
            next: TransportConnectionState.Disconnected,
            onTransition: () => this.Disconnected?.Invoke(this, EventArgs.Empty),
            allowNoOp: true);
    }

    public void OnFaulted(string message, Exception? exception = null)
    {
        lock (_sync)
        {
            if (_state == TransportConnectionState.Faulted)
            {
                return;
            }

            if (_state == TransportConnectionState.Disconnected)
            {
                throw InvalidTransition("Faulted");
            }

            _state = TransportConnectionState.Faulted;
        }

        this.Faulted?.Invoke(
            this,
            new TransportFaultedEventArgs(message, exception));
    }


    // ---------- Helpers ----------

    private void Transition(
        TransportConnectionState expected,
        TransportConnectionState next,
        Action onTransition)
    {
        lock (_sync)
        {
            if (_state == next)
                return; // benign duplicate

            if (_state != expected)
                throw InvalidTransition(next.ToString());

            _state = next;
        }

        onTransition();
    }

    private void TransitionAnyOf(
        TransportConnectionState[] allowedFrom,
        TransportConnectionState next,
        Action onTransition,
        bool allowNoOp)
    {
        lock (_sync)
        {
            if (_state == next && allowNoOp)
                return;

            if (!Array.Exists(allowedFrom, s => s == _state))
                throw InvalidTransition(next.ToString());

            _state = next;
        }

        onTransition();
    }

    private InvalidOperationException InvalidTransition(string target)
    {
        return new InvalidOperationException(
            $"Invalid connection state transition: {_state} → {target}");
    }
}
