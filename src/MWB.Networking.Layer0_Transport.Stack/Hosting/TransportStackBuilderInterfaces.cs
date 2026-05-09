using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport.Stack.Core.Connection;

namespace MWB.Networking.Layer0_Transport.Stack.Hosting;

/// <summary>
/// Stage 1 - Logger
/// </summary>
public interface ITransportStackBuilderLoggerStage
{
    ITransportStackBuilderConnectionProviderStage UseLogger(
        ILogger logger);
}

/// <summary>
/// Stage 2 - Connection provider
/// </summary>
public interface ITransportStackBuilderConnectionProviderStage
{
    ITransportStackBuilderOwnsProviderStage UseConnectionProvider(
        INetworkConnectionProvider connectionProvider);
}

/// <summary>
/// Stage 3 - Owns connection (optional)
/// </summary>
public interface ITransportStackBuilderOwnsProviderStage
{
    ITransportStackBuilderBuildStage OwnsProvider(
        bool ownsProvider = true);
    TransportStack Build();
}

/// <summary>
/// Stage 4 - Build
/// </summary>
public interface ITransportStackBuilderBuildStage
{
    TransportStack Build();
}
