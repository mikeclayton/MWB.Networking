using MWB.Networking.Layer1_Framing.Hosting;

namespace MWB.Networking.Layer1_Framing.Codecs.Default.Network;

public static class NetworkPipelineBuilderExtensions
{
    public static INetworkPipelineCodecStage UseDefaultNetworkCodec(
        this NetworkPipelineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder
            .UseNetworkCodec(
                () => new DefaultNetworkFrameCodec());
    }
}
