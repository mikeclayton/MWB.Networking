using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport.Stack.Core.Connection;

namespace MWB.Networking.Layer0_Transport.Stack.Hosting;

public sealed class TransportStackBuilderStages :
    ITransportStackBuilderLoggerStage,
    ITransportStackBuilderConnectionProviderStage,
    ITransportStackBuilderOwnsProviderStage,
    ITransportStackBuilderBuildStage
{
    internal TransportStackBuilderStages()
    {
    }

    // -----------------------------
    // Logger
    // -----------------------------

    private ILogger? _logger;

    public ITransportStackBuilderConnectionProviderStage UseLogger(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        return this;
    }

    // -----------------------------
    // Connection provider
    // -----------------------------

    private INetworkConnectionProvider? _connectionProvider;

    ITransportStackBuilderOwnsProviderStage ITransportStackBuilderConnectionProviderStage.UseConnectionProvider(
        INetworkConnectionProvider connectionProvider)
    {
        ArgumentNullException.ThrowIfNull(connectionProvider);

        _connectionProvider = connectionProvider;
        return this;
    }

    // -----------------------------
    // Owns provider
    // -----------------------------

    private bool _ownsProvider;

    ITransportStackBuilderBuildStage ITransportStackBuilderOwnsProviderStage.OwnsProvider(
        bool ownsProvider)
    {
        _ownsProvider = ownsProvider;
        return this;
    }

    TransportStack ITransportStackBuilderOwnsProviderStage.Build()
    {
        return ((ITransportStackBuilderBuildStage)this).Build();
    }

    // -----------------------------
    // Build
    // -----------------------------

    TransportStack ITransportStackBuilderBuildStage.Build()
    {
        var logger = _logger
            ?? throw new InvalidOperationException("A logger must be configured.");

        var connectionProvider = _connectionProvider
            ?? throw new InvalidOperationException("A connection provider must be configured.");

        return new TransportStack(logger, connectionProvider, _ownsProvider);
    }
}
