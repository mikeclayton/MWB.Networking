using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Streams.Publish;

public sealed class OutgoingStreamData
{
    public OutgoingStreamData(
        OutgoingStream stream,
        ReadOnlyMemory<byte> payload)
    {
        this.Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        this.Payload = payload;
    }

    public OutgoingStream Stream
    {
        get;
    }

    public ReadOnlyMemory<byte> Payload
    {
        get;
    }
}

