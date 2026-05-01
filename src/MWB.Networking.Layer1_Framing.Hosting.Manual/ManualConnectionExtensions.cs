using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport.Abstractions;
using MWB.Networking.Layer0_Transport.Manual;

namespace MWB.Networking.Layer1_Framing.Hosting.Manual;

public static class ManualConnectionExtensions
{
    ///// <summary>
    ///// Test-focused convenience for adapting an existing network connection
    ///// into a connection provider.
    /////
    ///// This is a thin wrapper over <see cref="UseManualNetworkConnectionProvider"/>,
    ///// intended to make test pipeline setup read in terms of intent rather than
    ///// provider mechanics.
    ///// </summary>
    //public static NetworkPipelineBuilder WrapConnectionAsProvider(
    //    this NetworkPipelineBuilder pipeline,
    //    ILogger logger, INetworkConnection connection)
    //{
    //    return pipeline.UseManualNetworkConnectionProvider(logger, connection);
    //}

    //public static NetworkPipelineBuilder UseManualNetworkConnectionProvider(
    //    this NetworkPipelineBuilder pipeline,
    //    ILogger logger, INetworkConnection connection)
    //{
    //    ArgumentNullException.ThrowIfNull(logger);
    //    ArgumentNullException.ThrowIfNull(connection);

    //    var provider = new ManualNetworkConnectionProvider(logger, connection);

    //    pipeline.UseConnectionProvider(provider);

    //    return pipeline;
    //}
}
