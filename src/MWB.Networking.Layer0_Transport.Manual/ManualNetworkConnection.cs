using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer0_Transport.Lifecycle.Abstractions;
using MWB.Networking.Layer0_Transport.Lifecycle.Stack;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace MWB.Networking.Layer0_Transport.Manual;

/// <summary>
/// Deterministic, manually driven test implementation of <see cref="INetworkConnection"/>.
///
/// Contains no background threads or I/O loops. All behavior is
/// explicitly controlled by test code.
///
/// Supports explicit lifecycle signaling via <see cref="OnStarted"/> and
/// <see cref="Disconnect"/>.
/// </summary>
public sealed class ManualNetworkConnection : INetworkConnection, IDisposable
{
    private readonly ObservableConnectionStatus _status;

    private readonly Channel<ReadOnlyMemory<byte>> _readChannel =
        Channel.CreateUnbounded<ReadOnlyMemory<byte>>(
            new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            });

    private readonly ConcurrentQueue<ByteSegments> _writes = new();

    private bool _started;
    private bool _isDisconnected;
    private bool _isDisposed;

    public ManualNetworkConnection(ObservableConnectionStatus status)
    {
        _status = status ?? throw new ArgumentNullException(nameof(status));
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
        if (_started)
            return;

        _started = true;

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

        _status.OnDisconnected();
    }

    // ------------------------------------------------------------------
    // INetworkConnection implementation
    // ------------------------------------------------------------------

    public async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken ct)
    {
        if (_isDisposed || _isDisconnected)
            return 0; // EOF

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
        data.Slice(0, length).CopyTo(buffer);

        return length;
    }

    public ValueTask WriteAsync(
        ByteSegments segments,
        CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(
            _isDisposed,
            nameof(ManualNetworkConnection));

        if (_isDisconnected)
            throw new InvalidOperationException(
                "Connection is disconnected.");

        _writes.Enqueue(segments);
        return ValueTask.CompletedTask;
    }

    // ------------------------------------------------------------------
    // Test instrumentation
    // ------------------------------------------------------------------

    /// <summary>
    /// Injects raw bytes that will be returned by the next
    /// <see cref="ReadAsync"/> call.
    /// </summary>
    public void InjectFrame(ReadOnlyMemory<byte> frame)
    {
        if (_isDisposed || _isDisconnected)
            return;

        _readChannel.Writer.TryWrite(frame);
    }

    /// <summary>
    /// Returns all data written via <see cref="WriteAsync"/>.
    /// </summary>
    public IReadOnlyCollection<ByteSegments> GetWrites()
        => _writes.ToArray();

    /// <summary>
    /// Indicates whether the connection has been manually disconnected.
    /// </summary>
    public bool IsDisconnected => _isDisconnected;

    // ------------------------------------------------------------------
    // Disposal
    // ------------------------------------------------------------------

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        _readChannel.Writer.TryComplete();
        _status.OnDisconnected();
    }
}