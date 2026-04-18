namespace MWB.Networking.Hosting;

/// <summary>
/// Application-facing builder for protocol sessions.
///
/// This type owns network pipeline wiring and delegates
/// final session creation to the protocol layer.
/// </summary>
public sealed partial class ProtocolSessionBuilder
{
    private Action<NetworkPipelineBuilder>? _pipelineConfig;

    /// <summary>
    /// Configures the network pipeline (encoders, transport).
    /// </summary>
    public ProtocolSessionBuilder ConfigurePipeline(
        Action<NetworkPipelineBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        this.EnsureNotBuilt();

        _pipelineConfig = configure;
        return this;
    }
}
