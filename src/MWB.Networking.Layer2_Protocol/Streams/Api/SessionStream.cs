using Microsoft.Extensions.Options;
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

    private ProtocolDirection Direction
        => this.Context.Direction;

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
    internal void Close()
        => this.Actions.Close(this.Context);

    /// <summary>
    /// Aborts the stream immediately and notifies the remote peer.
    /// </summary>
    public void Abort()
        => this.Actions.Abort(this.Context);
}
