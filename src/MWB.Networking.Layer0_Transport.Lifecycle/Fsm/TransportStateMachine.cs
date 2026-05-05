using MWB.Networking.Layer0_Transport.Lifecycle.Stack;

namespace MWB.Networking.Layer0_Transport.Lifecycle.Fsm;

internal sealed class TransportStateMachine
{
    public TransportStackState State
    { 
        get;
        private set;
    } = TransportStackState.Idle;

    public TransportStackTransition Process(TransportStackInputKind input, TransportFaultedEventArgs? e = null)
    {
        var transition = (State, input) switch
        {
            // ─────────────────────────────────────────────
            // IDLE
            // ─────────────────────────────────────────────

            (TransportStackState.Idle, TransportStackInputKind.ConnectRequested)
                => TransportStateMachine.Move(TransportStackState.Connecting, TransportConnectionState.Connecting),

            (TransportStackState.Idle, TransportStackInputKind.DisposeRequested)
                => TransportStateMachine.Move(TransportStackState.Terminated),

            // Ignore spurious provider signals while idle
            (TransportStackState.Idle, _) => Stay(),

            // ─────────────────────────────────────────────
            // CONNECTING
            // ─────────────────────────────────────────────

            (TransportStackState.Connecting, TransportStackInputKind.ProviderConnected)
                => TransportStateMachine.Move(TransportStackState.Connected, TransportConnectionState.Connected),

            (TransportStackState.Connecting, TransportStackInputKind.ProviderDisconnected)
                => TransportStateMachine.Recover(TransportConnectionState.Disconnected),

            (TransportStackState.Connecting, TransportStackInputKind.ProviderFaulted)
                => TransportStateMachine.Fault(e ?? throw new InvalidOperationException()),

            (TransportStackState.Connecting, TransportStackInputKind.DisconnectRequested)
                => TransportStateMachine.Move(TransportStackState.Disconnecting, TransportConnectionState.Disconnecting),

            (TransportStackState.Connecting, TransportStackInputKind.DisposeRequested)
                => TransportStateMachine.Move(
                    TransportStackState.Terminated,
                    TransportConnectionState.Disconnected,
                    TransportStackSideEffect.TearDownConnection),

            // ─────────────────────────────────────────────
            // CONNECTED
            // ─────────────────────────────────────────────

            (TransportStackState.Connected, TransportStackInputKind.DisconnectRequested)
                => TransportStateMachine.Move(TransportStackState.Disconnecting, TransportConnectionState.Disconnecting),

            (TransportStackState.Connected, TransportStackInputKind.ProviderDisconnected)
                => new TransportStackTransition(
                    nextState: TransportStackState.Idle,
                    publicState: TransportConnectionState.Disconnected,
                    sideEffect: TransportStackSideEffect.TearDownConnection),

            (TransportStackState.Connected, TransportStackInputKind.ProviderFaulted)
                => TransportStateMachine.Fault(e ?? throw new InvalidOperationException()),

            (TransportStackState.Connected, TransportStackInputKind.DisposeRequested)
                => TransportStateMachine.Move(
                    TransportStackState.Terminated,
                    TransportConnectionState.Disconnected,
                    TransportStackSideEffect.TearDownConnection),

            // ─────────────────────────────────────────────
            // DISCONNECTING
            // ─────────────────────────────────────────────

            (TransportStackState.Disconnecting, TransportStackInputKind.ProviderDisconnected)
                => TransportStateMachine.Recover(TransportConnectionState.Disconnected),

            (TransportStackState.Disconnecting, TransportStackInputKind.ProviderFaulted)
                => TransportStateMachine.Fault(e ?? throw new InvalidOperationException()),

            (TransportStackState.Disconnecting, TransportStackInputKind.DisposeRequested)
                => TransportStateMachine.Move(TransportStackState.Terminated, sideEffect: TransportStackSideEffect.TearDownConnection),

            // ─────────────────────────────────────────────
            // TERMINATED
            // ─────────────────────────────────────────────

            (TransportStackState.Terminated, _) => Stay(),

            // ─────────────────────────────────────────────
            // Defensive fallback (should never hit)
            // ─────────────────────────────────────────────

            _ => Stay()
        };

        // Commit state change only if one is specified
        if (transition.NextState is not null)
        {
            this.State = transition.NextState.Value;
        }

        return transition;
    }

    // ───────────── Helpers ─────────────

    private static TransportStackTransition Stay()
        => new(nextState: null);

    private static TransportStackTransition Move(
        TransportStackState next,
        TransportConnectionState? publicState = null,
        TransportStackSideEffect? sideEffect = null)
        => new(
            next,
            publicState,
            sideEffect: sideEffect);

    private static TransportStackTransition Recover(
        TransportConnectionState publicState)
        => new(
            TransportStackState.Idle,
            publicState,
            sideEffect: TransportStackSideEffect.TearDownConnection);

    private static TransportStackTransition Fault(TransportFaultedEventArgs e)
        => new(
            TransportStackState.Idle,
            TransportConnectionState.Faulted,
            e ?? throw new ArgumentNullException(nameof(e)),
            TransportStackSideEffect.TearDownConnection);
}
