using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Streams.Publish;

public sealed class OutgoingStreamOpened
{
    public OutgoingStreamOpened(
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
