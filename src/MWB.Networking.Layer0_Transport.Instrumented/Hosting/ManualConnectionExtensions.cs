using Microsoft.Extensions.Logging;

namespace MWB.Networking.Layer0_Transport.Instrumented.Hosting;

public static class InstrumentedConnectionExtensions
{
    /// <summary>
    /// Test-focused convenience for adapting an existing network connection
    /// into a connection provider.
    ///
    /// This is a thin wrapper over <see cref="UseManualNetworkConnectionProvider"/>,
    /// intended to make test pipeline setup read in terms of intent rather than
    /// provider mechanics.
    /// </summary>
    public static NetworkPipelineBuilder WrapConnectionAsProvider(
        this NetworkPipelineBuilder pipeline,
        ILogger logger, INetworkConnection connection)
    {
        return pipeline.UseManualNetworkConnectionProvider(logger, connection);
    }

    public static NetworkPipelineBuilder UseManualNetworkConnectionProvider(
        this NetworkPipelineBuilder pipeline,
        ILogger logger, INetworkConnection connection)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(connection);

        var provider = new ManualNetworkConnectionProvider(logger, connection);

        pipeline.UseConnectionProvider(provider);

        return pipeline;
    }
}
