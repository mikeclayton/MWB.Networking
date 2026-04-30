using MWB.Networking.Layer1_Framing.Defaults;

namespace MWB.Networking.Layer1_Framing.Hosting;

public static class INetworkPipelineBuilderExtensions
{
    public static INetworkPipelineCodecStage UseDefaultNetworkCodec(
        this NetworkPipelineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return new NetworkPipelineBuilderState()
            .UseNetworkCodec(
                () => new DefaultNetworkFrameCodec());
    }
}
