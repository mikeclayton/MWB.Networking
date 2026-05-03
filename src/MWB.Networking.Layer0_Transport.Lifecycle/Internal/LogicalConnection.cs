using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer0_Transport.Lifecycle.Abstractions;
using MWB.Networking.Layer0_Transport.Lifecycle.Stack;

namespace MWB.Networking.Layer0_Transport.Lifecycle.Internal;

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
    private bool _disposed;

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
        if (_status.State == TransportConnectionState.Connected)
        {
            return;
        }

        var tcs = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        void OnConnected(object? sender, EventArgs e)
            => tcs.TrySetResult();

        _status.Connected += OnConnected;
        try
        {
            if (_status.State == TransportConnectionState.Connected)
            {
                return;
            }

            using (ct.Register(() => tcs.TrySetCanceled(ct)))
            {
                await tcs.Task.ConfigureAwait(false);
            }
        }
        finally
        {
            _status.Connected -= OnConnected;
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
        if (_disposed)
            return;

        _disposed = true;
        _connection.Dispose();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(LogicalConnection));
    }
}