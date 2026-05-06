namespace MWB.Networking.Layer0_Transport.Segmented;

/// <summary>
/// Stream-style reader over a segmented memory buffer.
/// </summary>
public sealed class SegmentedBufferReader
{
    private readonly SegmentedBuffer _buffer;

    private byte[]? _current;
    private int _offset;

    internal SegmentedBufferReader(SegmentedBuffer buffer)
    {
        _buffer = buffer;
    }

    /// <summary>
    /// Reads bytes into the destination buffer.
    /// May return fewer bytes than requested.
    /// Returns 0 only on EOF.
    /// </summary>
    public async ValueTask<int> ReadAsync(
        Memory<byte> destination,
        CancellationToken ct)
    {
        // If we have no current segment (or it has been fully consumed),
        // acquire the next one from the buffer.
        if (_current == null || _offset >= _current.Length)
        {
            _current = await _buffer
                .DequeueAsync(ct)
                .ConfigureAwait(false);

            _offset = 0;

            if (_current == null)
            {
                return 0; // EOF
            }
        }

        // copy as much as fits into the destination buffer
        var bytesToCopy = Math.Min(
            destination.Length,
            _current.Length - _offset);

        _current
            .AsMemory(_offset, bytesToCopy)
            .CopyTo(destination);

        _offset += bytesToCopy;
        return bytesToCopy;
    }
}
