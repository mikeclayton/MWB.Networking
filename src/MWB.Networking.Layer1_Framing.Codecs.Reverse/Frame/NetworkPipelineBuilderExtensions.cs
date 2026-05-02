using MWB.Networking.Layer1_Framing.Pipeline.Hosting;

namespace MWB.Networking.Layer1_Framing.Codecs.Reverse.Frame;

public static class NetworkPipelineBuilderExtensions
{
    public static INetworkPipelineCodecStage UseReverseFrameCodec(
        this INetworkPipelineCodecStage builder)
    {
        return builder.UseFrameCodec(
            () => new ReverseFrameCodec());
    }
}
