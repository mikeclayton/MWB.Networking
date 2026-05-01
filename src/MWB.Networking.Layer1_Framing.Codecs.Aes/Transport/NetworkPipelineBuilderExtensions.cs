using MWB.Networking.Layer1_Framing.Hosting;

namespace MWB.Networking.Layer1_Framing.Codecs.Aes.Transport;

public static class NetworkPipelineBuilderExtensions
{
    public static INetworkPipelineBuildStage UseAesTransportCodec(
        this INetworkPipelineCodecStage builder)
    {
        return builder.UseTransportCodec(
            () => new AesTransportCodec());
    }
}
