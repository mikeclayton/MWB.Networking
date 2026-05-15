namespace MWB.Networking.Layer3_Endpoint.Hosting;

/// <summary>
/// Application-facing builder for protocol sessions.
///
/// This type owns network pipeline wiring and delegates
/// final session creation to the protocol layer.
/// </summary>
/// <remarks>
/// Each call to <see cref="Build"/> creates a completely new and isolated
/// protocol session object graph, including the pipeline, driver, queues,
/// observers, and background loops.
///
/// The builder is therefore reusable and may be treated as a session template
/// or factory. No endpoint objects are retained or shared between builds.
/// </remarks>
public sealed partial class SessionEndpointBuilder
{
    /// <summary>
    /// Builds and returns a fully wired session host.
    /// </summary>
    public SessionEndpoint Build()
    {
        if (_logger is null)
        {
            throw new InvalidOperationException(
                "Logger not configured. Call UseLogger().");
        }

        if (_streamIdParity is null)
        {
            throw new InvalidOperationException(
                "Stream ID parity not configured. Call UseOddStreamIds() or UseEvenStreamIds().");
        }

        if (_pipelineFactory is null)
        {
            throw new InvalidOperationException(
                "Network pipeline factory is not configured.");
        }

        if (_connectionProvider is null)
        {
            throw new InvalidOperationException(
                "Connection provider not configured. Call UseConnectionProvider().");
        }

        // ------------------------------------------------------------
        // Create protocol runtime objects (Layer 3 – lifecycle owner)
        // ------------------------------------------------------------

        var runtimeFactory =
            new ProtocolRuntimeFactory(
                _logger,
                _connectionProvider,
                _pipelineFactory,
                _streamIdParity.Value);

        var observers = this.BuildObservers();

        return new SessionEndpoint(
            _logger,
            runtimeFactory,
            observers);
    }
}
