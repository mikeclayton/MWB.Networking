using MWB.Networking.Layer0_Transport.Stack.Core.Connection;
using MWB.Networking.Layer0_Transport.Stack.Core.Lifecycle;
using MWB.Networking.Layer0_Transport.Stack.Core.Primitives;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace MWB.Networking.Layer0_Transport.Instrumented;

/// <summary>
/// Deterministic, manually driven test implementation of <see cref="INetworkConnection"/>.
///
/// Contains no background threads or I/O loops. All behavior is
/// explicitly controlled by test code.
///
/// Supports explicit lifecycle signaling via <see cref="OnStarted"/> and
/// <see cref="Disconnect"/>.
/// </summary>
public sealed partial class InstrumentedNetworkConnection : INetworkConnection, IDisposable
{
    private readonly IConnectionStatus _status;

    private readonly Channel<ReadOnlyMemory<byte>> _readChannel =
        Channel.CreateUnbounded<ReadOnlyMemory<byte>>(
            new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            });

    private readonly ConcurrentQueue<ByteSegments> _writes = new();

    private bool _isDisconnected;
    private bool _isFaulted;
    private volatile bool _disposed;

    private readonly bool _isLoopback;

    public InstrumentedNetworkConnection(IConnectionStatus status, bool isLoopback = false)
    {
        _status = status ?? throw new ArgumentNullException(nameof(status));
        _isLoopback = isLoopback;
        this.Instrumentation = new ConnectionInstrumentation(this);
    }

    // ------------------------------------------------------------------
    // Lifecycle (explicit, deterministic)
    // ------------------------------------------------------------------

    /// <summary>
    /// Signals that the connection has started.
    /// Test code controls when this occurs.
    /// </summary>
    internal void OnStarted()
    {
        _status.OnConnecting();
        _status.OnConnected();
    }

    /// <summary>
    /// Forces the connection into a disconnected (EOF) state.
    /// All subsequent reads return 0.
    /// </summary>
    public void Disconnect(string? reason = null)
    {
        if (_isDisconnected)
            return;

        _isDisconnected = true;

        _readChannel.Writer.TryComplete();

        _status.OnDisconnected(
            new TransportDisconnectedEventArgs(
                reason ?? "Manual transport disconnected by local request."));
    }

    // ------------------------------------------------------------------
    // INetworkConnection implementation
    // ------------------------------------------------------------------

    public async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken ct)
    {
        if (_disposed || _isFaulted)
        {
            return 0; // EOF
        }

        // NOTE: _isDisconnected is intentionally NOT checked here.
        // When Disconnect() is called it calls TryComplete() on the channel writer,
        // which lets any data already in the channel drain before the reader
        // gets ChannelClosedException (→ 0 / EOF).
        // Checking _isDisconnected upfront would cause buffered data to be lost.

        if (_nextReadFailure is { } ex)
        {
            _nextReadFailure = null;
            throw ex;
        }

        ReadOnlyMemory<byte> data;
        try
        {
            data = await _readChannel.Reader.ReadAsync(ct);
        }
        catch (ChannelClosedException)
        {
            return 0; // EOF
        }

        var length = Math.Min(data.Length, buffer.Length);
        data[..length].CopyTo(buffer);

        return length;
    }

    public ValueTask WriteAsync(
        ByteSegments segments,
        CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            nameof(InstrumentedNetworkConnection));

        if (_isDisconnected)
            throw new InvalidOperationException(
                "Connection is disconnected.");

        if (_isFaulted)
            throw new InvalidOperationException(
                "Connection is faulted.");

        if (_isLoopback)
        {
            // Loopback mode: route bytes directly to the read channel so that
            // ReadAsync returns them, simulating a round-trip transport.
            // _writes is NOT populated in this mode; each mode owns one buffer.

            // write the block as a single buffer to preserve segment boundaries in the loopback channel.
            segments = segments.Collapse();
            _readChannel.Writer.TryWrite(segments.Segments[0]);
        }
        else
        {
            // Normal mode: record bytes for test inspection via GetWrites().
            // _readChannel is only fed by explicit InjectBytes() calls.
            _writes.Enqueue(segments);
        }

        return ValueTask.CompletedTask;
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

        _readChannel.Writer.TryComplete();

        _status.OnDisconnected(
            new TransportDisconnectedEventArgs(
                "Manual transport disposed."));
    }
}