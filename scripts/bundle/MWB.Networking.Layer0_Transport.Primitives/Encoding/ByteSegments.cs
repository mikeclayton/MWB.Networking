namespace MWB.Networking.Layer0_Transport.Encoding;

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
