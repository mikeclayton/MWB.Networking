using MWB.Networking.Layer0_Transport.Encoding;

namespace MWB.Networking.Layer0_Transport.Memory;

/// <summary>
/// Buffered in-process implementation of <see cref="INetworkConnection"/>.
/// Provides stream semantics with explicit EOF on disposal.
/// </summary>
public sealed class InMemoryNetworkConnection : INetworkConnection, IDisposable
{
    private readonly MemoryBufferReader _reader;
    private readonly MemoryBufferWriter _writer;
    private bool _disposed;

    internal InMemoryNetworkConnection(
        MemoryBufferReader reader,
        MemoryBufferWriter writer)
    {
        _reader = reader;
        _writer = writer;
    }

    /// <summary>
    /// Writes a sequence of byte segments.
    /// Writes are buffered, non-blocking, and preserve segment boundaries.
    /// </summary>
    public async ValueTask WriteAsync(
        ByteSegments segments,
        CancellationToken cancellationToken)
    {
        this.ThrowIfDisposed();

        foreach (var segment in segments.Segments)
        {
            await _writer
                .WriteAsync(segment, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Writes a contiguous block of bytes.
    /// </summary>
    public ValueTask WriteAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken)
    {
        this.ThrowIfDisposed();
        return _writer.WriteAsync(data, cancellationToken);
    }

    /// <summary>
    /// Reads bytes into the provided buffer.
    /// May return fewer bytes than requested. Returns 0 on EOF.
    /// </summary>
    public ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken)
    {
        this.ThrowIfDisposed();
        return _reader.ReadAsync(buffer, cancellationToken);
    }

    /// <summary>
    /// Completes the write side and signals EOF to the reader.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _writer.Complete();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(InMemoryNetworkConnection));
        }
    }
}
