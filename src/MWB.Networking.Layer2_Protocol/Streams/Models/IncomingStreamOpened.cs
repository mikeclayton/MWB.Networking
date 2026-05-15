using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Streams.Models;

public sealed class IncomingStreamOpened
{
    public IncomingStreamOpened(
        IncomingStream stream,
        StreamMetadata metadata)
    {
        this.Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        this.Metadata = metadata;
    }

    public IncomingStream Stream
    {
        get;
    }

    public StreamMetadata Metadata
    {
        get;
    }
}
