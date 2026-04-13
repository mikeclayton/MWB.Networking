namespace MWB.Networking.Layer0_Transport;

public readonly struct NetworkBlock
{
    public NetworkBlock(ReadOnlyMemory<byte>[] segments)
    {
        this.Segments = segments ?? throw new ArgumentNullException(nameof(segments));

        var totalLength = 0;
        foreach (var s in segments)
        {
            totalLength += s.Length;
        }

        this.TotalLength = totalLength;
    }

    public ReadOnlyMemory<byte>[] Segments
    {
        get;
    }
    public int TotalLength
    {
        get;
    }
}
