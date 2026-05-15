using MWB.Networking.Layer0_Transport.Stack.Core.Lifecycle;

namespace MWB.Networking.Layer0_Transport.Stack.Fsm;

internal sealed class TransportStackTransition
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="nextState">The new internal lifecycle state</param>
    /// <param name="publicState">Optional ConnectionStateChanged emission</param>
    /// <param name="fault">Optional fault event to raise</param>
    /// <param name="sideEffect">Non‑state work (cleanup, dispose, etc.)</param>
    internal TransportStackTransition(
        TransportStackState? nextState,
        TransportConnectionState? publicState = null,
        TransportFaultedEventArgs? fault = null,
        TransportStackSideEffect? sideEffect = null)
    {
        this.NextState = nextState;
        this.PublicState = publicState;
        this.Fault = fault;
        this.SideEffect = sideEffect;
    }

    public TransportStackState? NextState
    {
        get;
    }
    
    public TransportConnectionState? PublicState
    {
        get;
    }

    public TransportFaultedEventArgs? Fault
    {
        get;
    }

    public TransportStackSideEffect? SideEffect
    {
        get;
    }
}
