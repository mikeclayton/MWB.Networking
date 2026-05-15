using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Streams.Models;

public sealed class StreamAbortedMessage
{
    public StreamAbortedMessage(
        SessionStream stream,
        StreamMetadata metadata)
    {
        this.Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        this.Metadata = metadata;
    }

    public SessionStream Stream
    {
        get;
    }

    public StreamMetadata Metadata
    {
        get;
    }
}
