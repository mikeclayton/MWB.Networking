namespace MWB.Networking.Layer0_Transport.Encoding;

/// <summary>
/// Represents a logical, atomic block of bytes at Layer 0.
/// </summary>
/// <remarks>
/// A single data payload may be accumulated from multiple independent operations
/// (for example, a byte range originating from multiple read operations). <see cref="ByteSegments"/>
/// preserves references to these individual segments to avoid the need to
/// allocate and copy them into a single contiguous memory buffer.
/// </remarks>
public readonly struct ByteSegments
{
    public ByteSegments(params ReadOnlyMemory<byte>[] segments)
    {
        this.Segments = segments ?? throw new ArgumentNullException(nameof(segments));
    }

    public ReadOnlyMemory<byte>[] Segments
    {
        get;
    }

    public ByteSegments Collapse()
    {
        // Fast path: already a single segment
        if (this.Segments.Length <= 1)
        {
            return this;
        }

        // Calculate total length
        var totalLength = 0;
        foreach (var segment in Segments)
        {
            totalLength += segment.Length;
        }

        // Allocate combined buffer
        var buffer = new byte[totalLength];
        var destination = buffer.AsSpan();

        // Copy segments
        var offset = 0;
        foreach (var segment in this.Segments)
        {
            segment.Span.CopyTo(destination[offset..]);
            offset += segment.Length;
        }

        return new ByteSegments(buffer);
    }
}
