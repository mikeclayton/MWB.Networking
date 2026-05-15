using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport.Stack.Core.Connection;
using MWB.Networking.Layer0_Transport.Stack.Core.Lifecycle;

namespace MWB.Networking.Layer0_Transport.Instrumented;

/// <summary>
/// Deterministic, manually driven test implementation of
/// <see cref="INetworkConnectionProvider"/>.
///
/// Returns a <see cref="InstrumentedNetworkConnection"/> whose behavior
/// is fully controlled by the test.
/// </summary>
public sealed partial class InstrumentedNetworkConnectionProvider
    : INetworkConnectionProvider, IDisposable
{
    private readonly ILogger _logger;
    private volatile bool _disposed;

    public InstrumentedNetworkConnectionProvider(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.Instrumentation = new ProviderInstrumentation(this);
    }

    /// <summary>
    /// Gets the most recently created manual connection.
    /// Tests use this to drive lifecycle and I/O deterministically.
    /// </summary>
    internal InstrumentedNetworkConnection? Connection
    {
        get;
        private set;
    }

    // ------------------------------------------------------------------
    // INetworkConnectionProvider implementation
    // ------------------------------------------------------------------
    
    public Task<INetworkConnection> OpenConnectionAsync(
        IConnectionStatus status,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        this.ThrowIfDisposed();

        // Simulate a provider-level connection failure if configured.
        if (_nextOpenConnectionFailure is { } failure)
        {
            _nextOpenConnectionFailure = null;
            throw failure;
        }

        var connection = new InstrumentedNetworkConnection(status, this.UseLoopback);

        // Expose to test code
        this.Connection = connection;
        _connections.Add(connection);

        // NOTE:
        // We deliberately do NOT call OnStarted() here.
        // Tests control exactly *when* the connection becomes connected.
        //
        // To drive the connection to Connected, call:
        //   provider.Connection!.OnStarted();          // Connecting + Connected
        //   provider.Connection!.SignalConnecting();   // Connecting only
        //   provider.Connection!.SignalConnected();    // Connected only

        return Task.FromResult<INetworkConnection>(connection);
    }

    // ------------------------------------------------------------------
    // Disposal
    // ------------------------------------------------------------------
    
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
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}