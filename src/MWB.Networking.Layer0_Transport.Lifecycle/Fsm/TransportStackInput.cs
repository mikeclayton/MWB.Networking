namespace MWB.Networking.Layer0_Transport.Lifecycle.Stack;

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

