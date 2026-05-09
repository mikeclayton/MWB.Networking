using MWB.Networking.Layer1_Framing.Pipeline.Hosting;

namespace MWB.Networking.Layer1_Framing.Codecs.Null.Transport.Hosting;

public static class NetworkPipelineBuilderExtensions
{
    public static INetworkPipelineBuilderBuildStage UseNullTransportCodec(
        this INetworkPipelineBuilderFrameCodecStage builder)
    {
        return builder.UseTransportFrameCodec(
            new NullTransportCodec()
        );
    }
}
