using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer2_Protocol.Events.Api;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Session.Api;

namespace MWB.Networking.Layer2_Protocol.Adapter;

public sealed partial class SessionAdapter :
    IIncomingActionSink,
    IOutgoingActionSink
{
    // ------------------------------------------------------------------
    // Incoming
    // ------------------------------------------------------------------

    public event Action<IncomingEvent>? IncomingEventReceived;
    public event Action<IncomingRequest>? IncomingRequestReceived;
    public event Action<IncomingResponse>? IncomingResponseReceived;

    void IIncomingActionSink.PublishIncomingEvent(IncomingEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);
        this.IncomingEventReceived?.Invoke(evt);
    }

    void IIncomingActionSink.PublishIncomingRequest(IncomingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        this.IncomingRequestReceived?.Invoke(request);
    }

    void IIncomingActionSink.PublishIncomingResponse(IncomingResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        this.IncomingResponseReceived?.Invoke(response);
    }

    // ------------------------------------------------------------------
    // Outgoing
    // ------------------------------------------------------------------

    public event Action<OutgoingEvent>? OutgoingEventSent;
    public event Action<OutgoingRequest>? OutgoingRequestSent;
    public event Action<OutgoingResponse>? OutgoingResponseSent;

    void IOutgoingActionSink.TransmitOutgoingEvent(OutgoingEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);
        var frame = NetworkFrames.Event(
            evt.EventType,
            evt.Payload);
        _transport.Send(frame);
        this.OutgoingEventSent?.Invoke(evt);
    }

    void IOutgoingActionSink.TransmitOutgoingRequest(OutgoingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var frame = NetworkFrames.Request(
            request.RequestId,
            request.RequestType,
            request.Payload);
        _transport.Send(frame);
        this.OutgoingRequestSent?.Invoke(request);
    }

    void IOutgoingActionSink.TransmitOutgoingResponse(OutgoingResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        var frame = NetworkFrames.Response(
            response.RequestId,
            response.ResponseType,
            response.Payload);
        _transport.Send(frame);
        this.OutgoingResponseSent?.Invoke(response);
    }
}
