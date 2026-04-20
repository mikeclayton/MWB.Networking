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
}
