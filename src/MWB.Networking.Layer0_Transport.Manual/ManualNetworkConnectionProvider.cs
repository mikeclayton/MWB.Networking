using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport.Lifecycle.Abstractions;
using MWB.Networking.Layer0_Transport.Lifecycle.Stack;

namespace MWB.Networking.Layer0_Transport.Manual;

/// <summary>
/// Deterministic, manually driven test implementation of
/// <see cref="INetworkConnectionProvider"/>.
///
/// Returns a <see cref="ManualNetworkConnection"/> whose behavior
/// is fully controlled by the test.
/// </summary>
public sealed class ManualNetworkConnectionProvider
    : INetworkConnectionProvider, IDisposable
{
    private readonly ILogger _logger;
    private bool _disposed;

    public ManualNetworkConnectionProvider(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the most recently created manual connection.
    /// Tests use this to drive lifecycle and I/O deterministically.
    /// </summary>
    public ManualNetworkConnection? Connection
    {
        get;
        private set;
    }

    public Task<INetworkConnection> OpenConnectionAsync(
        ObservableConnectionStatus status,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        this.ThrowIfDisposed();

        var connection = new ManualNetworkConnection(status);

        // Expose to test code
        this.Connection = connection;

        // NOTE:
        // We deliberately do NOT call OnStarted() here.
        // Tests control exactly *when* the connection becomes connected.
        //
        // If you want auto-start behavior, uncomment:
        //
        // connection.OnStarted();

        return Task.FromResult<INetworkConnection>(connection);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, true))
        {
            // was already disposed
            return;
        }

        _disposed = true;
        this.Connection?.Dispose();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ManualNetworkConnectionProvider));
    }
}