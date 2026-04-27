using MWB.Networking.Layer1_Framing.Hosting;
using MWB.Networking.Layer1_Framing.Pipeline;

namespace MWB.Networking.Layer3_Endpoint.Hosting;

public sealed partial class SessionEndpointBuilder
{
    private INetworkPipelineFactory? _pipelineFactory;

    /// <summary>
    /// Configures how network pipelines will be constructed
    /// when the endpoint starts.
    /// </summary>
    public SessionEndpointBuilder ConfigurePipelineWith(
        Action<NetworkPipelineBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        // 1. Create the Layer 1 builder
        var pipelineBuilder = new NetworkPipelineBuilder();
        // 2. Let caller configure it (unchanged DSL)
        configure(pipelineBuilder);
        // 3. Freeze configuration into a factory
        _pipelineFactory = pipelineBuilder.BuildFactory();
        return this;
    }
}
