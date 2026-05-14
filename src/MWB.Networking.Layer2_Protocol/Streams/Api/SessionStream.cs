using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Streams.Api;

public abstract class SessionStream
{
    internal SessionStream(
        StreamContext context,
        StreamActions actions)
    {
        this.Context = context ?? throw new ArgumentNullException(nameof(context));
        this.Actions = actions ?? throw new ArgumentNullException(nameof(actions));
    }

    private StreamContext Context
    {
        get;
    }

    public uint StreamId
        => this.Context.StreamId;

    public uint? StreamType
        => this.Context.StreamType;

    public StreamState StreamState
        => this.Context.StreamState;

    private StreamActions Actions
    {
        get;
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
        => this.Actions.Abort(this.Context, metadata);
}
