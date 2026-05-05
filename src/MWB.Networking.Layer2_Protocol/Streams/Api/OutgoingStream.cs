using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Streams.Api;

/// <summary>
/// Represents a stream initiated by the local peer.
/// Owns stream lifecycle and emits stream-related protocol frames
/// through the owning ProtocolSession.
/// </summary>
public sealed class OutgoingStream : IProtocolStream
{
    internal OutgoingStream(
        ProtocolSession session,
        uint streamId)
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

    private OutgoingStreamState State
    {
        get;
        set;
    } = OutgoingStreamState.Open;

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
    /// Closes the stream cleanly and notifies the peer.
    /// </summary>
    public void Close()
    {
        if (this.State != OutgoingStreamState.Open)
        {
            // we only close an open stream
            return;
        }

        this.State = OutgoingStreamState.Closed;
        this.Session.StreamManager.Outbound.CloseOutgoingStream(this.StreamId);
    }

    /// <summary>
    /// Closes the stream with an error and notifies the peer.
    /// </summary>
    public void Abort()
    {
        if (this.State != OutgoingStreamState.Open)
        {
            // we only abort an open stream
            return;
        }

        this.State = OutgoingStreamState.Aborted;
        this.Session.StreamManager.Outbound.AbortStream(this.StreamId);
    }

    private void EnsureOpen()
    {
        if (this.State != OutgoingStreamState.Open)
        {
            throw new InvalidOperationException(
                "Cannot operate on a closed or aborted stream.");
        }
    }

}
