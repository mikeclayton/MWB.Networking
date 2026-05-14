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
}
