using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport.NullTransport;

namespace MWB.Networking.Layer1_Framing.Hosting.NullTransport;

public static class NullConnectionExtensions
{
    public static NetworkPipelineBuilder UseNullConnectionProvider(
        this NetworkPipelineBuilder pipeline,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var provider = new NullConnectionProvider(logger);

        pipeline.UseConnectionProvider(provider);

        return pipeline;
    }
}
