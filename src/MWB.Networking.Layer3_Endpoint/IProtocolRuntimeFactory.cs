namespace MWB.Networking.Layer3_Endpoint;

/// <summary>
/// Creates the fully-wired runtime components required by a <see cref="SessionEndpoint"/>.
///
/// Implementations are responsible for constructing and wiring:
/// - The protocol session (<see cref="ProtocolRuntime.Session"/>)
/// - The frame-conversion adapter (<see cref="ProtocolRuntime.Adapter"/>)
/// - The transport driver (<see cref="ProtocolRuntime.Driver"/>)
///
/// All three must be ready to use when the returned <see cref="ProtocolRuntime"/>
/// is handed back; no further initialisation is performed by the endpoint itself
/// other than registering observers and calling <see cref="IProtocolDriver.Start"/>.
/// </summary>
public interface IProtocolRuntimeFactory
{
    Task<ProtocolRuntime> CreateAsync(CancellationToken ct);
}
