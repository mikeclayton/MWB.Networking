using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

internal sealed class StreamEntry
{
    public StreamEntry(StreamContext context, IncomingStream? incomingStream)
        : this(context, incomingStream ?? throw new ArgumentNullException(nameof(incomingStream)), null)
    {
    }

    public StreamEntry(StreamContext context, OutgoingStream? outgoingStream)
        : this(context, null, outgoingStream ?? throw new ArgumentNullException(nameof(outgoingStream)))
    {
    }

    private StreamEntry(StreamContext context, IncomingStream? incomingStream, OutgoingStream? outgoingStream)
    {
        this.Context = context;
        this.IncomingStream = incomingStream;
        this.OutgoingStream = outgoingStream;
    }

    public StreamContext Context
    {
        get;
    }

    public bool IsIncoming => this.IncomingStream is not null;

    public bool IsOutgoing => this.OutgoingStream is not null;

    public IncomingStream? IncomingStream
    {
        get;
    }

    public OutgoingStream? OutgoingStream
    {
        get;
    }

    public IncomingStream GetIncomingStreamOrThrow()
        => this.IncomingStream ?? throw new InvalidOperationException(
            $"{nameof(StreamEntry)} does not represent an incoming stream.");

    public OutgoingStream GetOutgoingStreamOrThrow()
        => this.OutgoingStream ?? throw new InvalidOperationException(
            $"{nameof(StreamEntry)} does not represent an outgoing stream.");
}
