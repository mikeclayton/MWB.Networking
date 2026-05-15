using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;

namespace MWB.Networking.Layer2_Protocol.Hosting;

/// <summary>
/// Semantic configuration for protocol session behavior.
/// </summary>
internal sealed class ProtocolSessionOptions
{
    public ProtocolSessionOptions(OddEvenStreamIdProvider outboundStreamIdProvider)
    {
        this.OutboundStreamIdProvider = outboundStreamIdProvider;
    }

    public OddEvenStreamIdProvider OutboundStreamIdProvider
    {
        get;
    }
}
