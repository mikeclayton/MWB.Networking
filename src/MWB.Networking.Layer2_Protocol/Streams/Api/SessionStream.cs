using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Streams.Api;

public abstract class SessionStream
{
    internal SessionStream(
        StreamContext context,
        StreamActions actions,
        ReadOnlyMemory<byte> payload,
        ProtocolDirection direction)
    {
        this.Context = context ?? throw new ArgumentNullException(nameof(context));
        this.Actions = actions ?? throw new ArgumentNullException(nameof(actions));
        this.Payload = payload;
        this.Direction = direction;
    }

    private StreamContext Context
    {
        get;
    }

    private StreamActions Actions
    {
        get;
    }

    public uint StreamId
        => this.Context.StreamId;

    public uint? StreamType
        => this.Context.StreamType;

    public ReadOnlyMemory<byte> Payload
    {
        get;
    }

    private ProtocolDirection Direction
    {
        get;
    }

    private IncomingStreamState State
    {
        get;
        set;
    } = IncomingStreamState.Open;

    private void EnsureOpen()
    {
        if (this.State != IncomingStreamState.Open)
        {
            throw new InvalidOperationException(
                "Cannot operate on a closed or aborted stream.");
        }
    }
    
    /// <summary>
    /// Sends data on this stream.
    /// </summary>
    public void SendData(ReadOnlyMemory<byte> payload)
    {
        this.EnsureOpen();

        this.Session.SendOutboundFrame(
            ProtocolFrames.StreamData(this.StreamId, payload));
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
