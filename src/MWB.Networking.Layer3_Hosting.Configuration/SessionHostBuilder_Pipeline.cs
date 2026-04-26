using MWB.Networking.Layer1_Framing.Hosting;

namespace MWB.Networking.Layer3_Hosting.Configuration;

/// <summary>
/// Application-facing builder for protocol sessions.
///
/// This type owns network pipeline wiring and delegates
/// final session creation to the protocol layer.
/// </summary>
public sealed partial class SessionHostBuilder
{
    private Action<NetworkPipelineFactory>? _pipelineFactory;

    /// <summary>
    /// Configures the network pipeline (encoders, transport).
    /// </summary>
    public SessionHostBuilder ConfigurePipeline(
        Action<NetworkPipelineFactory> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        _pipelineFactory = factory;
        return this;
    }
}
