using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer1_Framing.Hosting;
using MWB.Networking.Layer2_Protocol.Runtime;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer3_Hosting.Runtime;

namespace MWB.Networking.Layer3_Hosting.Configuration;

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
/// or factory. No runtime objects are retained or shared between builds.
/// </remarks>
public sealed partial class SessionHostBuilder
{
    /// <summary>
    /// Builds and returns a fully wired session host.
    /// </summary>
    public SessionHost Build()
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
                "Network pipeline factory is not configured. Call ConfigurePipeline().");
        }

        // ------------------------------------------------------------
        // Capture pipeline factory (NOT a pipeline instance)
        // ------------------------------------------------------------

        Func<CancellationToken, Task<NetworkPipeline>> pipelineFactory =
            async ct =>
            {
                var factory = new NetworkPipelineFactory();
                _pipelineFactory(factory);
                return await factory.CreatePipelineAsync(ct).ConfigureAwait(false);
            };

        // ------------------------------------------------------------
        // Create protocol runtime factory (Layer 2)
        // ------------------------------------------------------------


        Action<ProtocolSessionHandle>? applyObservers = null;
        if (_observerConfig is not null)
        {
            applyObservers =
                session => _observerConfig.ApplyObservers(session);
        }

        var instanceFactory =
            new ProtocolInstanceFactory(
                _logger,
                pipelineFactory,
                _streamIdParity.Value,
                applyObservers);

        // ------------------------------------------------------------
        // Create SessionHost (Layer 3 – lifecycle owner)
        // ------------------------------------------------------------

        return new SessionHost(
            _logger,
            instanceFactory);
    }
}
