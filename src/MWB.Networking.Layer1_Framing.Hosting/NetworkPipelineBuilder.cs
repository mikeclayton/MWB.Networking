using MWB.Networking.Layer1_Framing.Codec.Abstractions;

namespace MWB.Networking.Layer1_Framing.Hosting;

public sealed class NetworkPipelineBuilder
{
    // -----------------------------
    // Initial builder stage
    // -----------------------------

    public static INetworkPipelineCodecStage WithInitialCodec(
        Func<INetworkFrameCodec> networkFrameCodecFactory)
    {
        ArgumentNullException.ThrowIfNull(networkFrameCodecFactory);

        return new NetworkPipelineBuilderState()
            .UseNetworkCodec(networkFrameCodecFactory);
    }
}
