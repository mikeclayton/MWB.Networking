using MWB.Networking.Layer1_Framing.Pipeline.Hosting;

namespace MWB.Networking.Layer1_Framing.Codecs.Aes.Transport.Hosting;

public static class NetworkPipelineFactoryBuilderExtensions
{
    public static INetworkPipelineFactoryBuilderBuildStage UseAesTransportCodec(
        this INetworkPipelineFactoryBuilderFrameCodecStage builder)
    {
        return builder.UseTransportFrameCodec(
            () => new AesTransportCodec());
    }
}
