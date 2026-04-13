using MWB.Networking.Layer2_Protocol.Requests;
using System.Diagnostics.CodeAnalysis;

namespace MWB.Networking.Layer2_Protocol.Streams;

internal sealed class StreamContext
{
    private enum StreamState
    {
        Open,
        Closed
    }

    public StreamContext(uint streamId, RequestContext? owningRequest)
    {
        this.StreamId = streamId;
        this.OwningRequest = owningRequest;
    }

    private uint StreamId
    {
        get;
    }

    /// <summary>
    /// The Request that owns this Stream, if any.
    /// Null indicates a session-scoped Stream.
    /// </summary>
    internal RequestContext? OwningRequest
    {
        get;
    }

    [MemberNotNullWhen(true, nameof(OwningRequest))]
    internal bool IsRequestScoped
        => this.OwningRequest is not null;

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
        // If this is a request-scoped stream, the owning request must still be open
        this.OwningRequest?.EnsureOpen();
    }

    internal void Close()
    {
        if (this.State == StreamState.Closed)
        {
            throw new ProtocolException(
                ProtocolErrorKind.InvalidFrameSequence,
                $"Stream {StreamId} already closed");
        }
        this.State = StreamState.Closed;
    }
}
