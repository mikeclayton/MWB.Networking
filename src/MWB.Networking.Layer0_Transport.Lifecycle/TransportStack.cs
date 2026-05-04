using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer0_Transport.Lifecycle.Abstractions;
using MWB.Networking.Layer0_Transport.Lifecycle.Internal;

namespace MWB.Networking.Layer0_Transport.Lifecycle;

/// <summary>
/// Orchestrates the lifecycle of a network transport connection.
/// Owns connection creation, teardown, and state,
/// and exposes a logical byte-oriented connection surface.
/// </summary>
public sealed partial class TransportStack : IDisposable, IAsyncDisposable
{
    // -----------------------------
    // Construction
    // -----------------------------

    private readonly INetworkConnectionProvider _connectionProvider;
    private readonly object _sync = new();

    private LogicalConnection? _logicalConnection;
    private volatile bool _disposed;

    public TransportStack(
        INetworkConnectionProvider connectionProvider)
    {
        _connectionProvider =
            connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
    }

    /// <summary>
    /// Exposes the logical, ordered byte stream for this connection.
    /// Only valid while connected.
    /// </summary>
    private LogicalConnection LogicalConnection
    {
        get
        {
            lock (_sync)
            {
                if (_logicalConnection is null)
                    throw new InvalidOperationException("Transport is not connected.");

                return _logicalConnection;
            }
        }
    }

    // -----------------------------
    // Byte I/O surface
    // -----------------------------

    /// <summary>
    /// Asynchronously reads available bytes from the connection.
    /// </summary>
    public ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        LogicalConnection? conn;
        bool terminated;
        lock (_sync)
        {
            conn = _logicalConnection;
            terminated = this.ConnectionStatus?.HasTerminated ?? false;
        }
        if (conn is null)
        {
            // ConnectionStatus.HasTerminated distinguishes a real terminal
            // disconnect from the initial Disconnected state.
            if (!terminated)
                throw new InvalidOperationException("Transport is not connected.");

            // Was connected, now disconnected → EOF
            return new ValueTask<int>(0);
        }
        return conn.ReadAsync(buffer, cancellationToken);
    }

    /// <summary>
    /// Asynchronously writes bytes to the connection.
    /// </summary>
    public ValueTask WriteAsync(
        ByteSegments segments,
        CancellationToken cancellationToken = default)
        => this.LogicalConnection.WriteAsync(segments, cancellationToken);

    // -----------------------------
    // Disposal
    // -----------------------------

    void IDisposable.Dispose()
    {
        this.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, true))
        {
            return;
        }

        await this.DisconnectCoreAsync().ConfigureAwait(false);
        _connectionProvider.Dispose();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
