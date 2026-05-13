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

    internal StreamContext(
        uint streamId,
        uint? streamType,
        ProtocolDirection direction)
    {
        this.StreamId = streamId;
        this.StreamType = streamType;
        this.Direction = direction;
    }

    internal uint StreamId
    {
        get;
    }

    internal uint? StreamType
    {
        get;
    }

    internal ProtocolDirection Direction
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

    internal void EnsureIncoming()
    {
        if (this.Direction != ProtocolDirection.Incoming)
        {
            throw ProtocolException.ProtocolViolation(
                $"Stream {this.StreamId} is not inbound.");
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
