using MWB.Networking.Layer1_Framing.Pipeline;
using MWB.Networking.Layer1_Framing.Pipeline.Hosting;

namespace MWB.Networking.Layer3_Endpoint.Hosting;

public sealed partial class SessionEndpointBuilder
{
    private INetworkPipelineFactory? _pipelineFactory;

    /// <summary>
    /// Configures the network codec pipeline.
    ///
    /// The lambda receives a <see cref="NetworkPipelineBuilder"/> and must
    /// return the terminal <see cref="INetworkPipelineBuildStage"/> produced
    /// by the staged builder chain, for example:
    /// <code>
    ///   .ConfigurePipelineWith(pipeline =>
    ///       pipeline
    ///           .UseDefaultNetworkCodec()
    ///           .UseLengthPrefixedTransport(logger))
    /// </code>
    ///
    /// The connection provider is configured separately via
    /// <see cref="UseConnectionProvider"/>.
    /// </summary>
    public SessionEndpointBuilder UsePipeline(
        Func<INetworkPipelineFactoryBuilderLoggerStage, INetworkPipelineFactoryBuilderBuildStage> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var pipelineBuilder = NetworkPipelineFactoryBuilder.Create();
        _pipelineFactory = configure(pipelineBuilder).BuildFactory();
        return this;
    }
}
