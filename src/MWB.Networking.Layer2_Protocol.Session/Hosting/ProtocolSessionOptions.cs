using MWB.Networking.Layer2_Protocol.Session.Streams.Infrastructure;

namespace MWB.Networking.Layer2_Protocol.Session.Hosting;

/// <summary>
/// Semantic configuration for protocol session behavior.
/// </summary>
sealed class ProtocolSessionOptions
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
