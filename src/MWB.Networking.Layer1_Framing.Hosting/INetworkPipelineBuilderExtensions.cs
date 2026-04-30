using MWB.Networking.Layer1_Framing.Defaults;

namespace MWB.Networking.Layer1_Framing.Hosting;

public static class INetworkPipelineBuilderExtensions
{
    public static INetworkPipelineCodecStage UseDefaultNetworkCodec(
        this INetworkPipelineStartStage @interface)
    {
        ArgumentNullException.ThrowIfNull(@interface);
        return new NetworkPipelineBuilderState()
            .UseNetworkCodec(
                () => new DefautNetworkFrameCodec());
    }
}
