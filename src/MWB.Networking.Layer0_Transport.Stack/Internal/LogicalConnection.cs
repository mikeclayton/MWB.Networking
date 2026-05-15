using MWB.Networking.Layer0_Transport.Stack.Core.Connection;
using MWB.Networking.Layer0_Transport.Stack.Core.Lifecycle;
using MWB.Networking.Layer0_Transport.Stack.Core.Primitives;
using MWB.Networking.Layer0_Transport.Stack.Exceptions;
using MWB.Networking.Layer0_Transport.Stack.Lifecycle;

namespace MWB.Networking.Layer0_Transport.Stack.Internal;

/// <summary>
/// Provides a short‑lived logical I/O surface over a single physical
/// <see cref="INetworkConnection"/>.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="LogicalConnection"/> has a <em>mayfly</em> lifetime:
/// it is created for exactly one physical connection attempt and
/// disposed when that attempt ends.
/// </para>
/// <para>
/// This type does not participate in reconnection, attachment, or
/// lifecycle policy. It merely gates I/O until the underlying transport
/// reports <see cref="TransportConnectionState.Connected"/>.
/// </para>
/// </remarks>
internal sealed class LogicalConnection : IDisposable
{
    private readonly INetworkConnection _connection;
    private readonly ObservableConnectionStatus _status;
    private volatile bool _disposed;

    /// <summary>
    /// Initializes a new logical connection bound to a single
    /// physical network connection and its associated status.
    /// </summary>
    /// <param name="connection">
    /// The underlying network connection for this attempt.
    /// </param>
    /// <param name="status">
    /// The observable connection status governing readiness.
    /// </param>
    public LogicalConnection(
        INetworkConnection connection,
        ObservableConnectionStatus status)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _status = status ?? throw new ArgumentNullException(nameof(status));
    }

    /// <summary>
    /// Asynchronously waits until the underlying transport reports
    /// a connected state.
    /// </summary>
    /// <param name="ct">
    /// A cancellation token used to abort the wait.
    /// </param>

    private async Task AwaitConnectedAsync(CancellationToken ct)
    {
        // Fast-path
        if (_status.State == TransportConnectionState.Connected)
            return;

        // Lazily allocated only if we actually need to wait.
        // The handlers and ct.Register all call GetOrCreateTcs() which
        // uses Interlocked.CompareExchange to guarantee a single instance
        // even if multiple threads race to create it.
        TaskCompletionSource? tcs = null;

        TaskCompletionSource GetOrCreateTcs()
        {
            if (tcs is not null) return tcs;
            var newTcs = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            return Interlocked.CompareExchange(ref tcs, newTcs, null) ?? newTcs;
        }

        var cleanedUp = false;

        void Cleanup()
        {
            if (Interlocked.CompareExchange(ref cleanedUp, true, false))
            {
                // was already cleaned up
                return;
            }

            _status.Connected -= OnConnected;
            _status.Faulted -= OnFaulted;
            _status.Disconnected -= OnDisconnected;
        }

        void OnConnected(object? _, EventArgs __)
        {
            Cleanup();
            GetOrCreateTcs().TrySetResult();
        }

        void OnFaulted(object? _, TransportFaultedEventArgs e)
        {
            Cleanup();
            GetOrCreateTcs().TrySetException(
                new TransportFaultException(
                    "Logical connection faulted while awaiting connection.",
                    e));
        }

        void OnDisconnected(object? _, TransportDisconnectedEventArgs e)
        {
            Cleanup();
            GetOrCreateTcs().TrySetException(
                new TransportDisconnectedException(
                    "Logical connection disconnected while awaiting connection.",
                    e));
        }

        // Subscribe FIRST
        _status.Connected += OnConnected;
        _status.Faulted += OnFaulted;
        _status.Disconnected += OnDisconnected;

        try
        {
            // Re-check state AFTER subscribing (closes race window)
            switch (_status.State)
            {
                case TransportConnectionState.Connected:
                    Cleanup();
                    return; // no TCS allocated

                case TransportConnectionState.Faulted:
                    Cleanup();
                    throw new TransportFaultException(
                        "Logical connection faulted before becoming connected.",
                        new TransportFaultedEventArgs(
                            "Logical connection faulted before becoming connected."));

                case TransportConnectionState.Disconnected:

                    if (!_status.HasTerminated)
                    {
                        // initial state, not terminal yet
                        break;
                    }

                    Cleanup();
                    throw new TransportDisconnectedException(
                        "Logical connection disconnected before becoming connected.",
                        new TransportDisconnectedEventArgs(
                            "Logical connection disconnected before becoming connected."));

                default:
                    // Connecting / Disconnecting: wait for events
                    break;
            }

            using (ct.Register(() =>
            {
                Cleanup();
                GetOrCreateTcs().TrySetCanceled(ct);
            }))
            {
                await GetOrCreateTcs().Task.ConfigureAwait(false);
            }
        }
        finally
        {
            // Safety-net: ensure no leaked handlers
            Cleanup();
        }
    }


    /// <summary>
    /// Reads raw bytes from the underlying network connection.
    /// </summary>
    /// <remarks>
    /// This operation is gated until the connection reaches
    /// <see cref="TransportConnectionState.Connected"/>.
    /// </remarks>
    /// <param name="buffer">
    /// The destination buffer for received bytes.
    /// </param>
    /// <param name="ct">
    /// A cancellation token for the read operation.
    /// </param>
    /// <returns>
    /// The number of bytes read, or zero to indicate end‑of‑stream.
    /// </returns>
    public async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken ct)
    {
        this.ThrowIfDisposed();

        await AwaitConnectedAsync(ct).ConfigureAwait(false);
        return await _connection
            .ReadAsync(buffer, ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Writes raw byte segments to the underlying network connection.
    /// </summary>
    /// <remarks>
    /// This operation is gated until the connection reaches
    /// <see cref="TransportConnectionState.Connected"/>.
    /// </remarks>
    /// <param name="segments">
    /// The byte segments to transmit.
    /// </param>
    /// <param name="ct">
    /// A cancellation token for the write operation.
    /// </param>
    public async ValueTask WriteAsync(
        ByteSegments segments,
        CancellationToken ct)
    {
        this.ThrowIfDisposed();

        await AwaitConnectedAsync(ct).ConfigureAwait(false);
        await _connection
            .WriteAsync(segments, ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes the logical connection and the underlying
    /// network connection for this attempt.
    /// </summary>
    /// <remarks>
    /// Disposal permanently ends this logical connection and
    /// must not be followed by further I/O operations.
    /// </remarks>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, true))
        {
            // we were already disposed
            return;
        }

        _connection.Dispose();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}