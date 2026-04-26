using MWB.Networking.Layer0_Transport.Encoding;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace MWB.Networking.Layer0_Transport.Test;

/// <summary>
/// Deterministic, manually driven test implementation of <see cref="INetworkConnection"/>.
///
/// Contains no read or write loops or background processes - every action it takes
/// must be triggered by a method call.
///
/// Intended for protocol and transport unit tests that need explicit control
/// over reads, writes, and disconnection behavior.
/// </summary>
public sealed class ManualNetworkConnection : INetworkConnection
{
    private readonly Channel<ReadOnlyMemory<byte>> _readChannel =
        Channel.CreateUnbounded<ReadOnlyMemory<byte>>(
            new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            });

    private readonly ConcurrentQueue<ByteSegments> _writes =
        new();

    private volatile bool _isDisconnected;
    private volatile bool _isDisposed;

    // ------------------------------------------------------------------
    // INetworkConnection implementation
    // ------------------------------------------------------------------

    public async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken ct)
    {
        if (_isDisposed || _isDisconnected)
        {
            return 0; // EOF
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
        data.Slice(0, length).CopyTo(buffer);

        return length;
    }

    public ValueTask WriteAsync(
        ByteSegments segments,
        CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, nameof(ManualNetworkConnection));

        if (_isDisconnected)
        {
            throw new InvalidOperationException("Connection is disconnected.");
        }

        _writes.Enqueue(segments);
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        _isDisposed = true;
        _readChannel.Writer.TryComplete();
    }

    // ------------------------------------------------------------------
    // Test instrumentation (out-of-band control)
    // ------------------------------------------------------------------

    /// <summary>
    /// Injects raw bytes that will be returned by the next <see cref="ReadAsync"/> call.
    /// </summary>
    public void InjectFrame(ReadOnlyMemory<byte> frame)
    {
        if (_isDisposed) return;
        _readChannel.Writer.TryWrite(frame);
    }

    /// <summary>
    /// Forces the connection into an EOF state. All subsequent reads return 0.
    /// </summary>
    public void Disconnect()
    {
        _isDisconnected = true;
        _readChannel.Writer.TryComplete();
    }

    /// <summary>
    /// Returns all data written through <see cref="WriteAsync"/> so far.
    /// </summary>
    public IReadOnlyCollection<ByteSegments> GetWrites()
        => _writes.ToArray();

    /// <summary>
    /// Indicates whether the connection has been manually disconnected.
    /// </summary>
    public bool IsDisconnected => _isDisconnected;
}
