using MWB.Networking.Layer1_Framing.Pipeline;
using MWB.Networking.Layer2_Protocol.Driver;
using MWB.Networking.Layer2_Protocol.Session.Api;

namespace MWB.Networking.Layer2_Protocol.Runtime;

/// <summary>
/// Constructs a fully wired protocol instance
/// (session + execution driver) for a given network connection.
///
/// Implementations must create the session and driver atomically
/// and return a runnable execution unit.
/// </summary>
public interface IProtocolRuntimeFactory
{
    /// <summary>
    /// Creates a new protocol instance and its execution driver
    /// bound to the supplied network connection.
    /// </summary>
    public Task<(ProtocolSessionHandle Session, ProtocolDriver Driver, NetworkPipeline Pipeline)> CreateAsync(
        CancellationToken cancellationToken = default);
}
