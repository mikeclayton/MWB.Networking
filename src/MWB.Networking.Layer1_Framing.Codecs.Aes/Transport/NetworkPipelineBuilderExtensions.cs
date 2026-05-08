using MWB.Networking.Layer1_Framing.Pipeline.Hosting;

namespace MWB.Networking.Layer1_Framing.Codecs.Aes.Transport;

public static class NetworkPipelineBuilderExtensions
{
    public static INetworkPipelineBuildStage UseAesTransportCodec(
        this INetworkPipelineFrameCodecStage builder)
    {
        return builder.UseTransportFrameCodec(
            () => new AesTransportCodec());
    }
}
