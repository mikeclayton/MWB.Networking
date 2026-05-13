namespace MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

internal sealed class StreamActions
{
    internal StreamActions(StreamManager streamManager)
    {
        this.StreamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
    }

    private StreamManager StreamManager
    {
        get;
    }
}
