namespace MWB.Networking.Buffers.Segmented;

/// <summary>
/// Buffered, non-blocking writer for <see cref="SegmentedMemoryBuffer"/>.
/// </summary>
public sealed class SegmentedBufferWriter
{
    private readonly SegmentedBuffer _buffer;
    private bool _completed;

    internal SegmentedBufferWriter(SegmentedBuffer buffer)
    {
        _buffer = buffer;
    }

    /// <summary>
    /// Writes a single contiguous byte segment.
    /// </summary>
    public ValueTask WriteAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        if (_completed)
        {
            throw new InvalidOperationException("Writer already completed.");
        }

        // Copy once to preserve segment boundaries
        _buffer.Enqueue(data.ToArray());
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Completes the writer and signals EOF to the reader.
    /// </summary>
    public void Complete()
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        _buffer.Complete();
    }
}
