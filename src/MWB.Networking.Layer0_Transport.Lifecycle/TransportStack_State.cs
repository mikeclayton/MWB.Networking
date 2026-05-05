using MWB.Networking.Layer0_Transport.Lifecycle.Fsm;

namespace MWB.Networking.Layer0_Transport.Lifecycle;

public sealed partial class TransportStack
{
    private readonly TransportStateMachine _machine = new();
    private readonly object _sync = new();

    private void Apply(TransportStackTransition transition)
    {
        // Execute side-effects first (cleanup, disposal, etc.)
        switch (transition.SideEffect)
        {
            case TransportStackSideEffect.TearDownConnection:
                this.TearDownConnection();
                break;
        }

        // Raise fault event (if present)
        if (transition.Fault is not null)
        {
            this.RaiseFaultedEvent(transition.Fault);
        }

        // Raise public connection state change
        if (transition.PublicState is not null)
        {
            this.RaiseConnectionStateChanged(transition.PublicState.Value);
        }
    }
}
