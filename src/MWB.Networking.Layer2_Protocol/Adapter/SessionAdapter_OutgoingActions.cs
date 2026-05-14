using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer2_Protocol.Events.Api;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Session.Api;

namespace MWB.Networking.Layer2_Protocol.Adapter;

public sealed partial class SessionAdapter : IOutgoingActionSink
{
    // ------------------------------------------------------------------
    // Outgoing - Events
    // ------------------------------------------------------------------

    public event Action<OutgoingEvent>? OutgoingEventSent;

    void IOutgoingActionSink.TransmitOutgoingEvent(OutgoingEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);
        _queue.Writer.TryWrite(
            () => {
                var frame = NetworkFrames.Event(
                    evt.EventType,
                    evt.Payload);
                this.FrameSink.Send(frame);
                this.OutgoingEventSent?.Invoke(evt);
            });
    }

    // ------------------------------------------------------------------
    // Outgoing - Requests
    // ------------------------------------------------------------------

    public event Action<OutgoingRequest>? OutgoingRequestSent;
    public event Action<OutgoingResponse>? OutgoingResponseSent;

    void IOutgoingActionSink.TransmitOutgoingRequest(OutgoingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _queue.Writer.TryWrite(
            () =>
            {
                var frame = NetworkFrames.Request(
                    request.RequestId,
                    request.RequestType,
                    request.Payload);
                this.FrameSink.Send(frame);
                this.OutgoingRequestSent?.Invoke(request);
            });
    }

    void IOutgoingActionSink.TransmitOutgoingResponse(OutgoingResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        _queue.Writer.TryWrite(
            () =>
            {
                var frame = NetworkFrames.Response(
                    response.RequestId,
                    response.ResponseType,
                    response.Payload);
                this.FrameSink.Send(frame);
                this.OutgoingResponseSent?.Invoke(response);
            });
    }
}
