using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Streams.Publish;

public sealed class IncomingStreamClosed
{
    public IncomingStreamClosed(
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
