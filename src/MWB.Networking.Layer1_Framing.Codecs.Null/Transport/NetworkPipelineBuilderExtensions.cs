using MWB.Networking.Layer1_Framing.Pipeline.Hosting;

namespace MWB.Networking.Layer1_Framing.Codecs.Null.Transport;

public static class NetworkPipelineBuilderExtensions
{
    public static INetworkPipelineBuildStage UseLengthPrefixedTransport(
        this INetworkPipelineFrameCodecStage builder)
    {
        return builder.UseTransportFrameCodec(
            () => new NullTransportCodec()
        );
    }
}
