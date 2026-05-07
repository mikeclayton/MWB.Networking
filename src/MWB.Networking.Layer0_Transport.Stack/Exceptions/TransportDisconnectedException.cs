using MWB.Networking.Layer0_Transport.Stack.Lifecycle;

namespace MWB.Networking.Layer0_Transport.Stack.Exceptions;

/// <summary>
/// Raised when the transport enters the disconnected state.
/// Represents an observed transport failure.
/// </summary>
public sealed class TransportDisconnectedException : TransportException
{
    internal TransportDisconnectedException(string message, TransportDisconnectedEventArgs? eventArgs)
        : base(message, eventArgs?.Exception)
    {
        this.EventArgs = eventArgs;
    }

    public TransportDisconnectedEventArgs? EventArgs
    {
        get;
    }
}
