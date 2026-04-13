using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests;

namespace MWB.Networking.Layer2_Protocol;

public sealed partial class ProtocolSession : IProtocolRequestSink
{
    void IProtocolRequestSink.SendResponse(
        uint requestId,
        ReadOnlyMemory<byte> payload)
    {
        // NOTE:
        // We do not validate request existence here.
        // That responsibility belongs to RequestContext lifecycle management.
        //
        // At this point, the protocol has already accepted
        // that this request may be completed.

        var frame = ProtocolFrames.Response(
            requestId,
            payload);

        this.EnqueueOutboundFrame(frame);
    }

    void IProtocolRequestSink.SendError(
        uint requestId,
        ReadOnlyMemory<byte> payload)
    {
        var frame = ProtocolFrames.RequestError(
            requestId,
            payload);

        this.EnqueueOutboundFrame(frame);
    }

    void IProtocolRequestSink.SendCancel(uint requestId)
    {
        var frame = ProtocolFrames.CancelRequest(requestId);

        this.EnqueueOutboundFrame(frame);
    }

    void IProtocolRequestSink.CompleteRequest(uint requestId)
    {
        this.RemoveRequest(requestId);
    }
}
