using MWB.Networking.Layer1_Framing.Pipeline.Hosting;

namespace MWB.Networking.Layer1_Framing.Codecs.Aes.Transport.Hosting;

public static class NetworkPipelineBuilderExtensions
{
    public static INetworkPipelineBuilderBuildStage UseAesTransportCodec(
        this INetworkPipelineBuilderFrameCodecStage builder)
    {
        return builder.UseTransportFrameCodec(
            new AesTransportCodec());
    }
}
