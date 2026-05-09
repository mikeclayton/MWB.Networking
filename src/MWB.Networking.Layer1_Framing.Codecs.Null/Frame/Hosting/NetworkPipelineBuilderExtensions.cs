using MWB.Networking.Layer1_Framing.Pipeline.Hosting;

namespace MWB.Networking.Layer1_Framing.Codecs.Null.Frame.Hosting;

public static class NetworkPipelineBuilderExtensions
{
    public static INetworkPipelineBuilderFrameCodecStage UseNullFrameCodec(
        this INetworkPipelineBuilderFrameCodecStage builder)
    {
        return builder.UseFrameCodec(
            new NullFrameCodec());
    }
}
