using Microsoft.Extensions.Logging;

namespace MWB.Networking.Layer1_Framing.Pipeline.Hosting;

public sealed class NetworkPipelineBuilder
{
    // -----------------------------
    // Initial step
    // -----------------------------

    public INetworkPipelineBuilderNetworkCodecStage UseLogger(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        return new NetworkPipelineBuilderStages()
            .UseLogger(logger);
    }
}
