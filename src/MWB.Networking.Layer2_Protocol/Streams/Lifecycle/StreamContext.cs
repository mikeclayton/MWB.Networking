using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;
using System.Diagnostics.CodeAnalysis;

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

    internal StreamContext(uint streamId, uint? streamType, RequestContext? owningRequest)
    {
        this.StreamId = streamId;
        this.StreamType = streamType;
        this.OwningRequest = owningRequest;
    }

    internal uint StreamId
    {
        get;
    }

    internal uint? StreamType
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
                ProtocolErrorKind.InvalidSequence,
                $"Stream {this.StreamId} is closed");
        }
        // If this is a request-scoped stream, the owning request must still be open
        this.OwningRequest?.EnsureOpen();
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
