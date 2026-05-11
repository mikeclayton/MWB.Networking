using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Streams.Api;

public sealed class IncomingStream : IProtocolStream
{
    internal IncomingStream(
        ProtocolSession session,
        StreamContext context,
        IncomingRequest? owningRequest = null)
    {
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.Context = context ?? throw new ArgumentNullException(nameof(context));
        this.OwningRequest = owningRequest;
    }

    private ProtocolSession Session
    {
        get;
    }

    private StreamContext Context
    {
        get;
    }

    public uint StreamId
        => this.Context.StreamId;

    uint IProtocolStream.StreamId
        => this.StreamId;

    public uint? StreamType
        => this.Context.StreamType;

    private IncomingStreamState State
    {
        get;
        set;
    } = IncomingStreamState.Open;

    /// <summary>
    /// The request that owns this stream, if any.
    /// Null indicates a session-scoped stream.
    /// </summary>
    public IncomingRequest? OwningRequest
    {
        get;
    }

    /// <summary>
    /// Marks the stream as cleanly closed by the remote peer
    /// following receipt of a StreamClose frame.
    /// </summary>
    internal void Close()
    {
        if (this.State != IncomingStreamState.Open)
        {
            // we only abort an open stream
            return;
        }
        this.State = IncomingStreamState.Closed;
    }

    /// <summary>
    /// Marks the stream as cleanly closed by the remote peer
    /// following receipt of a StreamClose frame.
    /// </summary>
    void IProtocolStream.Close()
    {
        this.Close();
    }

    /// <summary>
    /// Abort this incoming stream due to a failure condition.
    /// This sends a StreamAbort frame to the peer and tears down local state.
    /// </summary>
    public void Abort()
    {
        if (this.State != IncomingStreamState.Open)
        {
            // we only abort an open stream
            return;
        }

        this.State = IncomingStreamState.Aborted;
        this.Session.StreamManager.Inbound.AbortIncomingStream(this.StreamId);
    }
}
