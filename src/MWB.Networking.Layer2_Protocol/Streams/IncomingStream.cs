namespace MWB.Networking.Layer2_Protocol.Streams;

public sealed class IncomingStream
{
    internal IncomingStream(IProtocolStreamSink streamSink, uint streamId)
    {
        this.StreamSink = streamSink ?? throw new ArgumentNullException(nameof(streamSink));
        this.StreamId = streamId;
    }

    private IProtocolStreamSink StreamSink
    {
        get;
    }

    private uint StreamId
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
        this.EnsureNotClosed();
        this.StreamSink.SendData(this.StreamId, dataPayload);
    }

    public void Close()
    {
        this.EnsureNotClosed();
        this.StreamSink.SendClose(this.StreamId);
        this.Closed = true;
    }

    public void Fail(ReadOnlyMemory<byte> errorPayload)
    {
        this.EnsureNotClosed();
        this.StreamSink.SendError(this.StreamId, errorPayload);
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
