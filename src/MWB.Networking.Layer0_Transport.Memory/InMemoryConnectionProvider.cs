using Microsoft.Extensions.Logging;

namespace MWB.Networking.Layer0_Transport.Memory;

/// <summary>
/// Provides one end of a duplex, buffered, in-memory network connection.
/// Intended for tests and single-process protocol harnesses.
/// </summary>
public sealed class InMemoryNetworkConnectionProvider
    : INetworkConnectionProvider, IDisposable
{
    private readonly ILogger _logger;
    private readonly InMemoryConnectionPair _pair;
    private readonly Side _side;
    private bool _opened;

    private InMemoryNetworkConnectionProvider(
        ILogger logger,
        InMemoryConnectionPair pair,
        Side side)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pair = pair ?? throw new ArgumentNullException(nameof(pair));
        _side = side;
    }

    /// <summary>
    /// Creates two paired providers representing opposite ends
    /// of a single duplex in-memory transport.
    /// </summary>
    public static (
        InMemoryNetworkConnectionProvider ProviderA,
        InMemoryNetworkConnectionProvider ProviderB)
        CreateDuplexProviders(ILogger logger)
    {
        var pair = new InMemoryConnectionPair();

        return (
            new InMemoryNetworkConnectionProvider(logger, pair, Side.A),
            new InMemoryNetworkConnectionProvider(logger, pair, Side.B)
        );
    }

    /// <summary>
    /// Opens the in-memory connection. May only be called once.
    /// </summary>
    public Task<LogicalConnectionHandle> OpenConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _opened, true))
        {
            throw new InvalidOperationException(
                "InMemoryNetworkConnectionProvider can only open one connection.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var connection =
            _side == Side.A
                ? _pair.ConnectionAtoB
                : _pair.ConnectionBtoA;

        var handle = LogicalConnectionFactory.Create(_logger);
        handle.Control.Attach(connection);

        return Task.FromResult(handle);
    }

    public void Dispose()
    {
        // Provider owns no resources beyond the logical handle.
        // Pair lifetime is shared and managed externally.
    }

    private enum Side
    {
        A,
        B
    }
}
