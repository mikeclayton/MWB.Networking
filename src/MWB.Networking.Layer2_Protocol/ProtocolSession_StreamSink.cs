using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Streams;

namespace MWB.Networking.Layer2_Protocol;

public sealed partial class ProtocolSession : IProtocolStreamSink
{
    void IProtocolStreamSink.SendData(
        uint streamId,
        ReadOnlyMemory<byte> payload)
    {
        var frame = ProtocolFrames.StreamData(
            streamId,
            payload);

        this.EnqueueOutboundFrame(frame);
    }

    void IProtocolStreamSink.SendClose(uint streamId)
    {
        var frame = ProtocolFrames.StreamClose(streamId);

        this.EnqueueOutboundFrame(frame);
    }

    void IProtocolStreamSink.SendError(
        uint streamId,
        ReadOnlyMemory<byte> payload)
    {
        var frame = ProtocolFrames.StreamError(
            streamId,
            payload);

        this.EnqueueOutboundFrame(frame);
    }
}
