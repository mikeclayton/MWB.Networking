using MWB.Networking.Layer1_Framing.Pipeline.Hosting;

namespace MWB.Networking.Layer1_Framing.Codecs.Null.Frame;

public static class NetworkPipelineBuilderExtensions
{
    public static INetworkPipelineFrameCodecStage UseNullFrameCodec(
        this INetworkPipelineFrameCodecStage builder)
    {
        return builder.UseFrameCodec(
            () => new NullFrameCodec());
    }
}
