namespace MWB.Networking.Layer2_Protocol.Streams;

public sealed class StreamContext
{
    private enum StreamState
    {
        Open,
        Closed
    }

    public StreamContext(uint streamId)
    {
        this.StreamId = streamId;
    }

    private uint StreamId
    {
        get;
    }

    private StreamState State
    {
        get;
        set;
    } = StreamState.Open;

    internal void EnsureOpen()
    {
        if (this.State != StreamState.Open)
        {
            throw new ProtocolException(
                ProtocolErrorKind.InvalidFrameSequence,
                $"Stream {this.StreamId} is closed");
        }
    }

    internal void MarkClosed()
    {
        this.State = StreamState.Closed;
    }
}
