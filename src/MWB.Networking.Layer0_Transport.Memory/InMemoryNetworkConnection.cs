using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer0_Transport.Lifecycle.Abstractions;
using MWB.Networking.Layer0_Transport.Lifecycle.Stack;
using MWB.Networking.Layer0_Transport.Memory.Buffer;

namespace MWB.Networking.Layer0_Transport.Memory;

/// <summary>
/// One endpoint of an in-memory full-duplex byte transport.
/// Wraps a reader and writer backed by a segmented buffer.
/// </summary>
/// <remarks>
/// This type contains no pairing, lifecycle policy, or reconnection logic.
/// Lifecycle is reported exclusively via <see cref="ObservableConnectionStatus"/>.
/// </remarks>
public sealed class InMemoryNetworkConnection :
    INetworkConnection,
    IDisposable
{
    private readonly SegmentedBufferReader _reader;
    private readonly SegmentedBufferWriter _writer;

    private ObservableConnectionStatus? _status;
    private bool _started;
    private volatile bool _disposed;

    internal InMemoryNetworkConnection(
        SegmentedBufferReader reader,
        SegmentedBufferWriter writer)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    // ------------------------------------------------------------------
    // Lifecycle binding (called by provider)
    // ------------------------------------------------------------------

    /// <summary>
    /// Binds the lifecycle status for this connection attempt.
    /// Must be called exactly once before <see cref="OnStarted"/>.
    /// </summary>
    internal void BindStatus(ObservableConnectionStatus status)
    {
        if (_status is not null)
            throw new InvalidOperationException("Status already bound.");

        _status = status ?? throw new ArgumentNullException(nameof(status));
    }

    /// <summary>
    /// Signals that wiring is complete and the endpoint is ready.
    /// In-memory transports are immediately usable.
    /// </summary>
    internal void OnStarted()
    {
        if (_started)
            return;

        _started = true;

        _status!.OnConnecting();
        _status.OnConnected();
    }

    // ------------------------------------------------------------------
    // INetworkConnection
    // ------------------------------------------------------------------

    public ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken ct)
    {
        ThrowIfDisposed();
        return _reader.ReadAsync(buffer, ct);
    }

    public async ValueTask WriteAsync(
        ByteSegments segments,
        CancellationToken ct)
    {
        ThrowIfDisposed();

        foreach (var segment in segments.Segments)
        {
            if (!segment.IsEmpty)
            {
                await _writer
                    .WriteAsync(segment, ct)
                    .ConfigureAwait(false);
            }
        }
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

        _writer.Complete();

        _status?.OnDisconnected(
              new TransportDisconnectedEventArgs(
                  "In-memory transport disposed."));
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
