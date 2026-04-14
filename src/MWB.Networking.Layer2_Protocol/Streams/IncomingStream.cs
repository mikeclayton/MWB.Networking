using MWB.Networking.Layer2_Protocol.Session;

namespace MWB.Networking.Layer2_Protocol.Streams;

public sealed class IncomingStream : IProtocolStream
{
    internal IncomingStream(ProtocolSession session, uint streamId)
    {
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.StreamId = streamId;
    }

    private ProtocolSession Session
    {
        get;
    }

    internal uint StreamId
    {
        get;
    }

    uint IProtocolStream.StreamId
        => this.StreamId;

    private IncomingStreamState State
    {
        get;
        set;
    } = IncomingStreamState.Open;

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
        this.Session.StreamManager.AbortIncomingStream(this.StreamId);
    }
}
