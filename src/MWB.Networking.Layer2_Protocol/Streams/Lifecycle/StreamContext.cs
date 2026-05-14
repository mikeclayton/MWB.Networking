using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

/// <summary>
/// Internal protocol invariant state for a stream.
/// Not an API object and not direction-specific.
/// </summary>
internal sealed class StreamContext
{
    private StreamContext(
        uint streamId,
        uint? streamType,
        ProtocolDirection direction)
    {
        this.StreamId = streamId;
        this.StreamType = streamType;
        this.Direction = direction;
    }

    internal static StreamContext CreateIncoming(
        uint streamId,
        uint? streamType,
        StreamActions actions)
    {
        var context = new StreamContext(streamId, streamType, ProtocolDirection.Incoming);
        var incomingStream = new IncomingStream(context, actions);
        context.IncomingStream = incomingStream;
        context.OutgoingStream = null;
        return context;
    }

    internal static StreamContext CreateOutgoing(
        uint streamId,
        uint? streamType,
        StreamActions actions)
    {
        var context = new StreamContext(streamId, streamType, ProtocolDirection.Outgoing);
        var outgoingStream = new OutgoingStream(context, actions);
        context.IncomingStream = null;
        context.OutgoingStream = outgoingStream;
        return context;
    }

    internal IncomingStream? IncomingStream
    {
        get;
        private set;
    }

    internal IncomingStream GetIncomingStream()
    {
        if ((this.Direction != ProtocolDirection.Incoming) || (this.IncomingStream is null))
        {
            throw ProtocolException.ProtocolViolation(
                $"Stream {this.StreamId} is not inbound.");
        }
        return this.IncomingStream;
    }

    internal OutgoingStream? OutgoingStream
    {
        get;
        private set;
    }

    internal OutgoingStream GetOutgoingStream()
    {
        if ((this.Direction != ProtocolDirection.Outgoing) || (this.OutgoingStream is null))
        {
            throw ProtocolException.ProtocolViolation(
                $"Stream {this.StreamId} is not outbound.");
        }
        return this.OutgoingStream;
    }

    internal uint StreamId
    {
        get;
    }

    internal uint? StreamType
    {
        get;
    }

    internal StreamState StreamState
    {
        get;
        private set;
    } = StreamState.None;

    internal bool IsFullyClosed
        => !this.StreamState.HasFlag(StreamState.Aborted)
        && this.StreamState.HasFlag(StreamState.LocalClosed | StreamState.RemoteClosed);

    internal ProtocolDirection Direction
    {
        get;
    }

    internal void EnsureCanSend()
    {
        if (this.StreamState.HasFlag(StreamState.Aborted))
        {
            throw new ProtocolException(
                ProtocolErrorKind.StreamAborted,
                $"Stream {StreamId} is aborted.");
        }
        if (this.StreamState.HasFlag(StreamState.LocalClosed))
        {
            throw new ProtocolException(
                ProtocolErrorKind.InvalidSequence,
                $"Stream {StreamId} has already closed its send direction.");
        }
    }

    internal void EnsureCanReceive()
    {
        if (this.StreamState.HasFlag(StreamState.Aborted))
        {
            throw new ProtocolException(
                ProtocolErrorKind.StreamAborted,
                $"Stream {StreamId} is aborted.");
        }

        if (this.StreamState.HasFlag(StreamState.RemoteClosed))
        {
            // THIS is the real invariant:
            // it's an error for the remote to send data after closing its half of the stream.
            throw new ProtocolException(
                ProtocolErrorKind.InvalidSequence,
                $"Stream {StreamId} received data after remote close.");
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

    /// <summary>
    /// Marks the stream as cleanly closed by the local peer.
    /// The local peer cannot send any more data, but can still receive.
    /// </summary>
    internal void CloseLocal()
    {
        if (this.StreamState.HasFlag(StreamState.Aborted))
        {
            throw ProtocolException.InvalidSequence($"Stream {StreamId} is aborted.");
        }
        if (this.StreamState.HasFlag(StreamState.LocalClosed))
        {
            // already closed — idempotent
            return;
        }
        this.StreamState |= StreamState.LocalClosed;   // callers check IsFullyClosed themselves
    }

    /// <summary>
    /// Marks the stream as cleanly closed by the remote peer.
    /// The remote peer cannot send any more data, but can still receive.
    /// </summary>
    internal void CloseRemote()
    {
        if (this.StreamState.HasFlag(StreamState.Aborted))
        {
            throw ProtocolException.InvalidSequence($"Stream {StreamId} is aborted.");
        }
        this.StreamState |= StreamState.RemoteClosed;
    }

    /// <summary>
    /// Abort this stream due to a failure condition signalled by
    /// either the local or remote peer.
    /// </summary>
    internal void Abort()
    {
        // callers publish and remove themselves
        this.StreamState = StreamState.Aborted;
    }
}
