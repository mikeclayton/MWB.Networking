using Microsoft.Extensions.Logging;

namespace MWB.Networking.Layer1_Framing.Pipeline.Hosting;

public sealed class NetworkPipelineBuilder
{
    // -----------------------------
    // Initial builder stage
    // -----------------------------

    public INetworkPipelineNetworkCodecStage UseLogger(
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        return new NetworkPipelineBuilderState()
            .UseLogger(logger);
    }
}
