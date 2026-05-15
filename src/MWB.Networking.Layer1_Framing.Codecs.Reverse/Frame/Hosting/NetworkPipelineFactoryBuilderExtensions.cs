using MWB.Networking.Layer1_Framing.Pipeline.Hosting;

namespace MWB.Networking.Layer1_Framing.Codecs.Reverse.Frame;

public static class NetworkPipelineFactoryBuilderExtensions
{
    public static INetworkPipelineFactoryBuilderFrameCodecStage UseReverseFrameCodec(
        this INetworkPipelineFactoryBuilderFrameCodecStage builder)
    {
        return builder.UseFrameCodec(
            () => new ReverseFrameCodec());
    }
}
