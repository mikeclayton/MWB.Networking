using MWB.Networking.Layer0_Transport.Encoding;

namespace MWB.Networking.Layer0_Transport.Memory;

public sealed class MemoryNetworkConnection : INetworkConnection, IDisposable
{
    private readonly MemoryStream _stream;

    public MemoryNetworkConnection(int initialCapacity = 1024 * 1024)
    {
        // Pre-size to reduce resize noise during benchmarks
        _stream = new MemoryStream(initialCapacity);
    }

    /// <summary>
    /// Reads raw bytes from the connection.
    /// Not supported for this in-memory benchmark connection.
    /// </summary>
    public ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken ct)
        => throw new NotSupportedException(
            "MemoryNetworkConnection does not support reading.");

    /// <summary>
    /// Writes raw byte segments to the underlying memory buffer.
    /// </summary>
    public ValueTask WriteAsync(
        ByteSegments segments,
        CancellationToken ct)
    {
        // IMPORTANT:
        // Intentionally synchronous.
        // This connection is for throughput measurement, not realism.
        foreach (var segment in segments.Segments)
        {
            if (!segment.IsEmpty)
            {
                _stream.Write(segment.Span);
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Number of bytes written so far (for benchmark inspection).
    /// </summary>
    public long BytesWritten => _stream.Length;

    public void Dispose()
    {
        _stream.Dispose();
    }
}