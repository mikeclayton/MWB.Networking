namespace MWB.Networking.Layer3_Endpoint;

/// <summary>
/// Abstraction over a transport driver as seen by <see cref="SessionEndpoint"/>.
///
/// Implementations are responsible for:
/// - Starting the read-and-decode loop on demand
/// - Releasing all I/O resources on disposal
///
/// The concrete implementation in the current stack is <c>TransportDriver</c>
/// from <c>MWB.Networking.Layer0_Transport.Driver</c>.
/// </summary>
public interface IProtocolDriver : IDisposable
{
    /// <summary>
    /// Starts the driver's I/O loop. Must be called exactly once after construction.
    /// </summary>
    void Start();
}
