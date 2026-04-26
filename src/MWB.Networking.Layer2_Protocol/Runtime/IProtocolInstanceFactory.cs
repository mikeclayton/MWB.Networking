namespace MWB.Networking.Layer2_Protocol.Runtime;

/// <summary>
/// Constructs a fully wired protocol instance
/// (session + execution driver) for a given network connection.
///
/// Implementations must create the session and driver atomically
/// and return a runnable execution unit.
/// </summary>
public interface IProtocolInstanceFactory
{
    /// <summary>
    /// Creates a new protocol instance and its execution driver
    /// bound to the supplied network connection.
    /// </summary>
    Task<ProtocolInstance> CreateAsync(CancellationToken cancellationToken);
}
