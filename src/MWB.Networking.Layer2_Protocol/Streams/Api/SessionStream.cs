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

    private protected StreamContext Context
    {
        get;
    }

    public uint StreamId
        => this.Context.StreamId;

    public uint? StreamType
        => this.Context.StreamType;

    public StreamState StreamState
        => this.Context.StreamState;

    private protected StreamActions Actions
    {
        get;
    }
}
