using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

/// <summary>
/// Holds the identity and lifecycle state of a protocol stream,
/// from opening through closure or abort.
/// </summary>
internal sealed partial class StreamContext
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

    internal SessionStream GetSessionStream()
    {
        return (SessionStream?)this.IncomingStream
            ?? this.OutgoingStream
            ?? throw new InvalidOperationException("Stream not bound.");
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
}
