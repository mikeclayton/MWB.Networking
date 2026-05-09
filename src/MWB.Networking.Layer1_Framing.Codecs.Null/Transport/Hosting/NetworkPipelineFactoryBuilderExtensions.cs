using MWB.Networking.Layer1_Framing.Pipeline.Hosting;

namespace MWB.Networking.Layer1_Framing.Codecs.Null.Transport.Hosting;

public static class NetworkPipelineFactoryBuilderExtensions
{
    public static INetworkPipelineFactoryBuilderBuildStage UseNullTransportCodec(
        this INetworkPipelineFactoryBuilderFrameCodecStage builder)
    {
        return builder.UseTransportFrameCodec(
            () => new NullTransportCodec()
        );
    }
}
