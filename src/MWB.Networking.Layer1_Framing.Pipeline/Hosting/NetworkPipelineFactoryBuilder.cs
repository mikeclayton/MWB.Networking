using Microsoft.Extensions.Logging;

namespace MWB.Networking.Layer1_Framing.Pipeline.Hosting;

public sealed class NetworkPipelineFactoryBuilder
{
    internal NetworkPipelineFactoryBuilder()
    {
    }

    // -----------------------------
    // Initial step
    // -----------------------------

    public INetworkPipelineFactoryBuilderNetworkCodecStage UseLogger(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        return new NetworkPipelineFactoryBuilderStages()
            .UseLogger(logger);
    }
}
