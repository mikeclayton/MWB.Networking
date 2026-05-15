using MWB.Networking.Layer1_Framing.Pipeline.Hosting;

namespace MWB.Networking.Layer1_Framing.Codecs.Reverse.Frame;

public static class NetworkPipelineBuilderExtensions
{
    public static INetworkPipelineBuilderFrameCodecStage UseReverseFrameCodec(
        this INetworkPipelineBuilderFrameCodecStage builder)
    {
        return builder.UseFrameCodec(
            new ReverseFrameCodec());
    }
}
