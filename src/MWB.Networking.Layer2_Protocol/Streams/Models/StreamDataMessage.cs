using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Streams.Models;

public sealed class StreamDataMessage
{
    public StreamDataMessage(
        SessionStream stream,
        ReadOnlyMemory<byte> payload)
    {
        this.Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        this.Payload = payload;
    }

    public SessionStream Stream
    {
        get;
    }

    public ReadOnlyMemory<byte> Payload
    {
        get;
    }
}

