using MWB.Networking.Layer1_Framing.Pipeline.Hosting;

namespace MWB.Networking.Layer1_Framing.Codecs.Default.Network.Hosting;

public static class NetworkPipelineBuilderExtensions
{
    public static INetworkPipelineBuilderFrameCodecStage UseDefaultNetworkCodec(
        this INetworkPipelineBuilderNetworkCodecStage builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder
            .UseNetworkFrameCodec(
                new DefaultNetworkFrameCodec());
    }
}
