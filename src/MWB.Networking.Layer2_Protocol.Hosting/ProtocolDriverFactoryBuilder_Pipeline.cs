using MWB.Networking.Layer1_Framing.Pipeline;

namespace MWB.Networking.Layer2_Protocol.Hosting;

/// <summary>
/// Application-facing builder for protocol sessions.
///
/// This type owns network pipeline wiring and delegates
/// final session creation to the protocol layer.
/// </summary>
public sealed partial class ProtocolDriverFactoryBuilder
{
    private Func<CancellationToken, Task<NetworkPipeline>>? _pipelineFactory;

    /// <summary>
    /// Configures the factory that builds network pipelines (encoders, transport).
    /// </summary>
    public ProtocolDriverFactoryBuilder UsePipelineFactory(
        Func<CancellationToken, Task<NetworkPipeline>> pipelineFactory)
    {
        ArgumentNullException.ThrowIfNull(pipelineFactory);
        _pipelineFactory = pipelineFactory;
        return this;
    }

    /// <summary>
    /// Configures the factory that builds network pipelines (encoders, transport).
    /// </summary>
    public ProtocolDriverFactoryBuilder UsePipelineFactory(
        Action<NetworkPipelineFactory> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        _pipelineFactory = async ct =>
        {
            var builder = new NetworkPipelineFactory();
            configure(builder);
            return await builder.CreatePipelineAsync(ct).ConfigureAwait(false);
        };

        return this;
    }
}
