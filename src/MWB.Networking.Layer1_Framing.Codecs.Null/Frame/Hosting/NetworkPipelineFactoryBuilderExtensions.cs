using MWB.Networking.Layer1_Framing.Pipeline.Hosting;

namespace MWB.Networking.Layer1_Framing.Codecs.Null.Frame.Hosting;

public static class NetworkPipelineFactoryBuilderExtensions
{
    public static INetworkPipelineFactoryBuilderFrameCodecStage UseNullFrameCodec(
        this INetworkPipelineFactoryBuilderFrameCodecStage builder)
    {
        return builder.UseFrameCodec(
            () => new NullFrameCodec());
    }
}
