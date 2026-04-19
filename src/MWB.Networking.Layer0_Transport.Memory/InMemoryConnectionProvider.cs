namespace MWB.Networking.Layer0_Transport.Memory;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Provides one end of a duplex, buffered, in‑memory network connection.
/// Intended for tests and single‑process protocol harnesses.
/// </summary>
public sealed class InMemoryNetworkConnectionProvider
{
    private readonly INetworkConnection _connection;
    private bool _opened;

    private InMemoryNetworkConnectionProvider(INetworkConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Creates two paired providers representing opposite ends
    /// of a single duplex in‑memory transport.
    /// </summary>
    public static (
        InMemoryNetworkConnectionProvider ProviderA,
        InMemoryNetworkConnectionProvider ProviderB)
        CreateDuplexProviders()
    {
        var pair = new InMemoryConnectionPair();

        return (
            new InMemoryNetworkConnectionProvider(pair.ConnectionAtoB),
            new InMemoryNetworkConnectionProvider(pair.ConnectionBtoA)
        );
    }

    /// <summary>
    /// Opens the in‑memory connection. May only be called once.
    /// </summary>
    public ValueTask<INetworkConnection> OpenConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _opened, true))
        {
            throw new InvalidOperationException(
                "InMemoryNetworkConnectionProvider can only open one connection.");
        }

        return new ValueTask<INetworkConnection>(_connection);
    }
}
