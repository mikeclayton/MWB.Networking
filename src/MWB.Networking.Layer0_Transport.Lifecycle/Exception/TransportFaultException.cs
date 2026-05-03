namespace MWB.Networking.Layer0_Transport.Lifecycle.Stack;

/// <summary>
/// Raised when the transport enters the Faulted state.
/// Represents an observed transport failure.
/// </summary>
public sealed class TransportFaultException : TransportException
{
    internal TransportFaultException(string message, TransportFaultedEventArgs? fault = null)
        : base(message, fault?.Exception)
    {
        this.Fault = fault;
    }

    public TransportFaultedEventArgs? Fault
    {
        get;
    }
}
