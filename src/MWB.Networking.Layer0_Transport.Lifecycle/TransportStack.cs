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
    private ObservableConnectionStatus? _status;
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
    // Lifecycle operations
    // -----------------------------

    /// <summary>
    /// Establishes a new network connection using the configured provider.
    /// </summary>
    public async Task ConnectAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        ObservableConnectionStatus status;
        INetworkConnection physicalConnection;
        LogicalConnection logical;

        lock (_sync)
        {
            if (_logicalConnection is not null)
            {
                throw new InvalidOperationException("Transport is already connected.");
            }

            status = new ObservableConnectionStatus();
            _status = status;
        }

        // Request a physical connection attempt
        physicalConnection =
            await _connectionProvider
                .OpenConnectionAsync(status, cancellationToken)
                .ConfigureAwait(false);

        // Create the logical connection
        logical = new LogicalConnection(physicalConnection, status);

        // Wire lifecycle events
        status.Connected += this.OnConnected;
        status.Disconnected += this.OnDisconnected;
        status.Faulted += this.OnFaulted;

        lock (_sync)
        {
            if (_disposed)
            {
                logical.Dispose();
                throw new ObjectDisposedException(nameof(TransportStack));
            }

            _logicalConnection = logical;
        }
    }

    /// <summary>
    /// Gracefully disconnects the current connection.
    /// </summary>
    public Task DisconnectAsync(
        CancellationToken cancellationToken = default)
    {
        this.ThrowIfDisposed();

        LogicalConnection? logical;
        ObservableConnectionStatus? status;

        lock (_sync)
        {
            logical = _logicalConnection;
            status = _status;

            _logicalConnection = null;
            _status = null;
        }

        if (status is not null)
        {
            status.Connected -= OnConnected;
            status.Disconnected -= OnDisconnected;
            status.Faulted -= OnFaulted;
        }

        logical?.Dispose();
        return Task.CompletedTask;
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
    // Events
    // -----------------------------

    /// <summary>
    /// Raised when the connection is successfully established.
    /// </summary>
    public event EventHandler? Connected;

    /// <summary>
    /// Raised when the connection is closed or lost.
    /// </summary>
    public event EventHandler? Disconnected;

    /// <summary>
    /// Raised when a fatal transport error occurs.
    /// </summary>
    public event EventHandler<TransportFaultedEventArgs>? Faulted;

    private void OnConnected(object? sender, EventArgs e)
        => this.Connected?.Invoke(this, EventArgs.Empty);

    private void OnDisconnected(object? sender, EventArgs e)
    {
        lock (_sync)
        {
            _logicalConnection = null;
            _status = null;
        }

        this.Disconnected?.Invoke(this, e);
    }

    private void OnFaulted(object? sender, TransportFaultedEventArgs e)
        => this.Faulted?.Invoke(this, e);

    // -----------------------------
    // Disposal
    // -----------------------------

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        this.DisconnectAsync().GetAwaiter().GetResult();
        _connectionProvider.Dispose();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(TransportStack));
    }
}
