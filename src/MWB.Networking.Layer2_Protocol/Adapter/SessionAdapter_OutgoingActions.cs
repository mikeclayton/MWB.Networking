using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer2_Protocol.Events.Api;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Streams.Publish;

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

    // ------------------------------------------------------------------
    // Outgoing - Streams
    // ------------------------------------------------------------------

    public event Action<OutgoingStreamOpened>? OutgoingStreamOpened;
    public event Action<OutgoingStreamData>? OutgoingStreamData;
    public event Action<OutgoingStreamClosed>? OutgoingStreamClosed;
    public event Action<OutgoingStreamAborted>? OutgoingStreamAborted;

    public void TransmitOutgoingStreamOpened(OutgoingStreamOpened streamOpened)
    {
        ArgumentNullException.ThrowIfNull(streamOpened);
        _queue.Writer.TryWrite(
            () =>
            {
                var outgoingStream = streamOpened.Stream;
                var frame = NetworkFrames.Response(
                    outgoingStream.StreamId,
                    outgoingStream.StreamType,
                    streamOpened.Metadata.Payload);
                this.FrameSink.Send(frame);
                this.OutgoingStreamOpened?.Invoke(streamOpened);
            });
    }

    public void TransmitOutgoingStreamData(OutgoingStreamData streamData)
    {
        ArgumentNullException.ThrowIfNull(streamData);
        _queue.Writer.TryWrite(
            () =>
            {
                var outgoingStream = streamData.Stream;
                var frame = NetworkFrames.Response(
                    outgoingStream.StreamId,
                    outgoingStream.StreamType,
                    streamData.Payload);
                this.FrameSink.Send(frame);
                this.OutgoingStreamData?.Invoke(streamData);
            });
    }

    public void TransmitOutgoingStreamClosed(OutgoingStreamClosed streamClosed)
    {
        ArgumentNullException.ThrowIfNull(streamClosed);
        _queue.Writer.TryWrite(
            () =>
            {
                var outgoingStream = streamClosed.Stream;
                var frame = NetworkFrames.Response(
                    outgoingStream.StreamId,
                    outgoingStream.StreamType,
                    streamClosed.Metadata.Payload);
                this.FrameSink.Send(frame);
                this.OutgoingStreamClosed?.Invoke(streamClosed);
            });
    }

    public void TransmitOutgoingStreamAborted(OutgoingStreamAborted streamAborted)
    {
        ArgumentNullException.ThrowIfNull(streamAborted);
        _queue.Writer.TryWrite(
            () =>
            {
                var outgoingStream = streamAborted.Stream;
                var frame = NetworkFrames.Response(
                    outgoingStream.StreamId,
                    outgoingStream.StreamType,
                    streamAborted.Metadata.Payload);
                this.FrameSink.Send(frame);
                this.OutgoingStreamAborted?.Invoke(streamAborted);
            });
    }
}
