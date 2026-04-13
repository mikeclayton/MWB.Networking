namespace MWB.Networking.Layer2_Protocol.Streams;

internal sealed class StreamEntry
{
    public StreamEntry(StreamContext context, IncomingStream stream)
    {
        this.Context = context;
        this.Stream = stream;
    }

    public StreamContext Context
    {
        get;
    }

    public IncomingStream Stream
    {
        get;
    }
}
