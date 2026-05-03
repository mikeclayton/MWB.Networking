using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer0_Transport.Lifecycle.Abstractions;
using MWB.Networking.Layer0_Transport.Lifecycle.Internal;
using MWB.Networking.Layer0_Transport.Lifecycle.Stack;

namespace MWB.Networking.Layer0_Transport.Lifecycle;

/// <summary>
/// Orchestrates the lifecycle of a network transport connection.
/// Owns connection creation, teardown, and state,
/// and exposes a logical byte-oriented connection surface.
/// </summary>
public sealed partial class TransportStack : IDisposable
{
    // -----------------------------
    // Construction
    // -----------------------------

    private readonly INetworkConnectionProvider _connectionProvider;
    private readonly object _sync = new();

    private LogicalConnection? _logicalConnection;
    private bool _disposed;

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
        => this.LogicalConnection.ReadAsync(buffer, cancellationToken);

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

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, true))
        {
            // _disposed was already true before, so exit
            return;
        }

        this.DisconnectAsync().GetAwaiter().GetResult();
        _connectionProvider.Dispose();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(TransportStack));
    }
}
