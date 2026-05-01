using MWB.Networking.Layer1_Framing.Hosting;

namespace MWB.Networking.Layer1_Framing.Codecs.Gzip.Frame;

public static class NetworkPipelineBuilderExtensions
{
    public static INetworkPipelineCodecStage UseNullFrameCodec(
        this INetworkPipelineCodecStage builder)
    {
        return builder.UseFrameCodec(
            () => new GzipFrameCodec());
    }
}
