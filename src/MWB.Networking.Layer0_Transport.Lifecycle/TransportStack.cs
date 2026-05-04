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
    private readonly bool _ownsProvider;

    private LogicalConnection? _logicalConnection;
    private volatile bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="TransportStack"/> with the given connection provider.
    /// </summary>
    /// <param name="connectionProvider">
    /// The provider used to open physical network connections.
    /// </param>
    /// <param name="ownsProvider">
    /// <see langword="true"/> (the default) if this stack should dispose
    /// <paramref name="connectionProvider"/> when the stack itself is disposed;
    /// <see langword="false"/> if the caller retains ownership of the provider's lifetime.
    /// </param>
    public TransportStack(
        INetworkConnectionProvider connectionProvider,
        bool ownsProvider = true)
    {
        _connectionProvider =
            connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _ownsProvider = ownsProvider;
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
        bool hasEverConnected;

        lock (_sync)
        {
            conn = _logicalConnection;
            hasEverConnected = _hasEverConnected;
        }

        if (conn is null)
        {
            // distinguish "never connected" vs "EOF"
            if (hasEverConnected)
            {
                return new ValueTask<int>(0); // EOF
            }
            // Otherwise this is a logic error: read before connected
            throw new InvalidOperationException(
                "Transport is not connected.");
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
        if (_ownsProvider)
        {
            _connectionProvider.Dispose();
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
