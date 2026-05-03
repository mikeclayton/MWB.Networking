namespace MWB.Networking.Layer0_Transport.Lifecycle.Stack;

/// <summary>
/// Base type for all transport-layer exceptions.
/// </summary>
public class TransportException : Exception
{
    internal TransportException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
