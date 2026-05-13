using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Streams.Api;

public sealed class IncomingStream : SessionStream
{
    internal IncomingStream(
        StreamContext context,
        StreamActions actions,
        ReadOnlyMemory<byte> payload)
        : base(context, actions, payload, ProtocolDirection.Incoming)
    {
    }
}
