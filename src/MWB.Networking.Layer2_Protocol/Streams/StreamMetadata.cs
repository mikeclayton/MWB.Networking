namespace MWB.Networking.Layer2_Protocol.Streams;

public sealed class StreamMetadata
{
    public StreamMetadata(ReadOnlyMemory<byte> payload)
    {
        this.Payload = payload;
    }

    public ReadOnlyMemory<byte> Payload
    {
        get;
    }
}
