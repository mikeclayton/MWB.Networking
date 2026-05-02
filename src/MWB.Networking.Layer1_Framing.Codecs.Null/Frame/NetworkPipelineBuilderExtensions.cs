using MWB.Networking.Layer1_Framing.Pipeline.Hosting;

namespace MWB.Networking.Layer1_Framing.Codecs.NullCodecs.Frame;

public static class NetworkPipelineBuilderExtensions
{
    public static INetworkPipelineCodecStage UseNullFrameCodec(
        this INetworkPipelineCodecStage builder)
    {
        return builder.UseFrameCodec(
            () => new NullFrameCodec());
    }
}
