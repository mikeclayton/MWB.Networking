using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Streams.Models;

public sealed class OutgoingStreamClosed
{
    public OutgoingStreamClosed(
        OutgoingStream stream,
        StreamMetadata metadata)
    {
        this.Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        this.Metadata = metadata;
    }

    public OutgoingStream Stream
    {
        get;
    }

    public StreamMetadata Metadata
    {
        get;
    }
}
