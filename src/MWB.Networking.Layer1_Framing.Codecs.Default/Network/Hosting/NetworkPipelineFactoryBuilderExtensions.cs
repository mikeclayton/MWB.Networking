using MWB.Networking.Layer1_Framing.Pipeline.Hosting;

namespace MWB.Networking.Layer1_Framing.Codecs.Default.Network.Hosting;

public static class NetworkPipelineFactoryBuilderExtensions
{
    public static INetworkPipelineFactoryBuilderFrameCodecStage UseDefaultNetworkCodec(
        this INetworkPipelineFactoryBuilderNetworkCodecStage builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder
            .UseNetworkFrameCodec(
                () => new DefaultNetworkFrameCodec());
    }
}
