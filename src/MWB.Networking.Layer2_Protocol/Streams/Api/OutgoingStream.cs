using MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Streams.Api;

public sealed class OutgoingStream : SessionStream
{
    internal OutgoingStream(
        StreamContext context,
        StreamActions actions)
        : base(context, actions)
    {
    }

    /// <summary>
    /// Sends data on this stream.
    /// </summary>
    public void SendData(ReadOnlyMemory<byte> payload)
        => this.Actions.SendData(this.Context, payload);

    /// <summary>
    /// Cleanly closes this stream and notifies the peer.
    /// </summary>
    public void Close(ReadOnlyMemory<byte> metadata = default)
        => this.Actions.Close(this.Context, metadata);

    /// <summary>
    /// Aborts the stream immediately and notifies the remote peer.
    /// </summary>
    public void Abort(ReadOnlyMemory<byte> metadata = default)
        => this.Actions.AbortOutgoing(this.Context, metadata);
}
