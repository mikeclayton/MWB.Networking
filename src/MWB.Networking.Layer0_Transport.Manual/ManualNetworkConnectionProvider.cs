using Microsoft.Extensions.Logging;

namespace MWB.Networking.Layer0_Transport.Manual;

/// <summary>
/// Deterministic, manually driven test implementation of
/// <see cref="INetworkConnectionProvider"/>.
///
/// Intended for unit tests that need explicit control over
/// connection attachment, replacement, and disconnection,
/// without background activity or timing dependencies.
/// </summary>
public sealed class ManualNetworkConnectionProvider
    : INetworkConnectionProvider, IDisposable
{
    private readonly LogicalConnectionHandle _handle;
    private INetworkConnection? _pendingConnection;
    private bool _disposed;

    public ManualNetworkConnectionProvider(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _handle = LogicalConnectionFactory.Create(logger);
    }

    public ManualNetworkConnectionProvider(
        ILogger logger,
        INetworkConnection connection)
        : this(logger)
    {
        _pendingConnection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <summary>
    /// Exposes the logical connection handle for wiring protocol sessions in tests.
    /// </summary>
    public LogicalConnectionHandle Handle => _handle;

    /// <inheritdoc />
    public Task<LogicalConnectionHandle> OpenConnectionAsync(
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        if (_pendingConnection is not null)
        {
            _handle.Control.Attach(_pendingConnection);
            _pendingConnection = null;
        }

        return Task.FromResult(_handle);
    }

    // ------------------------------------------------------------
    // Test instrumentation
    // ------------------------------------------------------------

    /// <summary>
    /// Attaches a physical network connection to the logical connection.
    /// Equivalent to a provider accepting or establishing a connection.
    /// </summary>
    public void Attach(INetworkConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ThrowIfDisposed();
        _handle.Control.Attach(connection);
    }

    /// <summary>
    /// Replaces the currently attached physical connection with a new one.
    /// Uses the same attach mechanism as production providers.
    /// </summary>
    public void Replace(INetworkConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ThrowIfDisposed();
        _handle.Control.Attach(connection);
    }

    // ------------------------------------------------------------
    // Disposal
    // ------------------------------------------------------------

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Terminate the logical connection and any active protocol sessions.
        _handle.Connection.Dispose();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ManualNetworkConnectionProvider));
    }
}
