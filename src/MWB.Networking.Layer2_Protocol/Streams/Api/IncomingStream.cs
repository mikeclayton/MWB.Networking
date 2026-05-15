using MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Streams.Api;

public sealed class IncomingStream : SessionStream
{
    internal IncomingStream(
        StreamContext context,
        StreamActions actions)
        : base(context, actions)
    {
    }

    /// <summary>
    /// Aborts the stream immediately and notifies the remote peer.
    /// </summary>
    public void Abort(ReadOnlyMemory<byte> metadata = default)
        => this.Actions.AbortIncoming(this.Context, metadata);
}
