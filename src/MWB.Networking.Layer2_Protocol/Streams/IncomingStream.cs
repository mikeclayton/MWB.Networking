namespace MWB.Networking.Layer2_Protocol.Streams;

public sealed class IncomingStream
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

    private bool Closed
    {
        get;
        set;
    }

    public void SendData(ReadOnlyMemory<byte> dataPayload)
    {
        this.Session.SendStreamData(this.StreamId, dataPayload);
    }

    public void Close()
    {
        this.EnsureNotClosed();
        this.Session.CloseStream(this.StreamId);
        this.Closed = true;
    }

    public void Fail(ReadOnlyMemory<byte> errorPayload)
    {
        this.EnsureNotClosed();
        this.Session.CloseStreamWithError(this.StreamId, errorPayload);
        this.Closed = true;
    }

    private void EnsureNotClosed()
    {
        if (this.Closed)
        {
            throw new InvalidOperationException("Stream already closed.");
        }
    }
}
