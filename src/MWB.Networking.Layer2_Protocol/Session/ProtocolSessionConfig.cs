using MWB.Networking.Layer2_Protocol.Lifecycle.Infrastructure;

namespace MWB.Networking.Layer2_Protocol.Session;

/// <summary>
/// Semantic configuration for protocol session behavior.
/// </summary>
public sealed class ProtocolSessionConfig
{
    public ProtocolSessionConfig(OddEvenStreamIdProvider outboundStreamIdProvider)
    {
        this.OutboundStreamIdProvider = outboundStreamIdProvider;
    }

    public OddEvenStreamIdProvider OutboundStreamIdProvider
    {
        get;
    }
}
