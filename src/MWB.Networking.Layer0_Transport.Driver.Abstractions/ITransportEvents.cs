namespace MWB.Networking.Layer0_Transport.Driver.Abstractions;

public interface ITransportEvents
{
    /// <summary>
    /// Raised when the transport closes cleanly (EOF).
    /// </summary>
    event Action TransportClosed;

    /// <summary>
    /// Raised when the transport faults.
    /// </summary>
    event Action<Exception> TransportFaulted;
}
