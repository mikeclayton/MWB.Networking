using MWB.Networking.Layer2_Protocol.Internal;

namespace MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

/// <summary>
/// Internal protocol invariant state for a stream.
/// Not an API object and not direction-specific.
/// </summary>
internal sealed class StreamContext
{
    private enum StreamState
    {
        Open,
        Closed
    }

    internal StreamContext(uint streamId, uint? streamType)
    {
        this.StreamId = streamId;
        this.StreamType = streamType;
    }

    internal uint StreamId
    {
        get;
    }

    internal uint? StreamType
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
                ProtocolErrorKind.InvalidSequence,
                $"Stream {this.StreamId} is closed");
        }
    }

    internal void Close()
    {
        if (this.State == StreamState.Closed)
        {
            return;
        }
        this.State = StreamState.Closed;
    }
}
