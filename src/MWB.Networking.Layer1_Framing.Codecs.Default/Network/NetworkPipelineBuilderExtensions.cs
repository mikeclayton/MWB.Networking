using MWB.Networking.Layer1_Framing.Pipeline.Hosting;

namespace MWB.Networking.Layer1_Framing.Codecs.Default.Network;

public static class NetworkPipelineBuilderExtensions
{
    public static INetworkPipelineFrameCodecStage UseDefaultNetworkCodec(
        this INetworkPipelineNetworkCodecStage builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder
            .UseNetworkFrameCodec(
                () => new DefaultNetworkFrameCodec());
    }
}
