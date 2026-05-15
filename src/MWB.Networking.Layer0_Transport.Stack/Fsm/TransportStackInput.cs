using MWB.Networking.Layer0_Transport.Stack.Core.Lifecycle;
using MWB.Networking.Layer0_Transport.Stack.Lifecycle;

namespace MWB.Networking.Layer0_Transport.Stack.Fsm;

internal sealed class TransportStackInput
{
    internal TransportStackInput(
        TransportStackInputKind Kind,
        TransportFaultedEventArgs? Fault = null)
    {
        this.Kind = Kind;
        this.Fault = Fault;
    }

    TransportStackInputKind Kind
    {
        get;
    }

    TransportFaultedEventArgs? Fault
    {
        get;
    }
}

