using MWB.Networking.Layer2_Protocol.Frames;

namespace MWB.Networking.UnitTest.Helpers.Layer2_Protocol;

internal static class ProtocolFrameGenerator
{
    internal static ProtocolFrame CreateInvalidProtocolFrame(
        ProtocolFrameKind kind,
        uint? eventType = null,
        uint? requestId = null,
        uint? requestType = null,
        uint? streamId = null,
        uint? streamType = null,
        ReadOnlyMemory<byte> payload = default)
    {
        return new ProtocolFrame(
            kind: kind,
            eventType: eventType,
            requestId: requestId,
            requestType: requestType,
            streamId: streamId,
            streamType: streamType,
            payload: payload
        );
    }
}
