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
}
