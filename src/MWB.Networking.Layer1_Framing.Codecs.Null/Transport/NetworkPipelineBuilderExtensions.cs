using MWB.Networking.Layer1_Framing.Pipeline.Hosting;

namespace MWB.Networking.Layer1_Framing.Codecs.NullCodecs.Transport;

public static class NetworkPipelineBuilderExtensions
{
    public static INetworkPipelineBuildStage UseLengthPrefixedTransport(
        this INetworkPipelineCodecStage builder)
    {
        return builder.UseTransportCodec(
            () => new NullTransportCodec()
        );
    }
}
