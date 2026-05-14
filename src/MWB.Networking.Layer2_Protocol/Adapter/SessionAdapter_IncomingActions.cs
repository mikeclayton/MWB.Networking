using MWB.Networking.Layer2_Protocol.Events.Api;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Streams.Publish;

namespace MWB.Networking.Layer2_Protocol.Adapter;

public sealed partial class SessionAdapter : IIncomingActionSink
{
    // ------------------------------------------------------------------
    // Incoming - Events
    // ------------------------------------------------------------------

    public event Action<IncomingEvent>? IncomingEventReceived;

    void IIncomingActionSink.PublishIncomingEvent(IncomingEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);
        _queue.Writer.TryWrite(
            () => this.IncomingEventReceived?.Invoke(evt));
    }

    // ------------------------------------------------------------------
    // Incoming - Requests
    // ------------------------------------------------------------------

    public event Action<IncomingRequest>? IncomingRequestReceived;
    public event Action<IncomingResponse>? IncomingResponseReceived;

    void IIncomingActionSink.PublishIncomingRequest(IncomingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _queue.Writer.TryWrite(
            () => this.IncomingRequestReceived?.Invoke(request));
    }

    void IIncomingActionSink.PublishIncomingResponse(IncomingResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        _queue.Writer.TryWrite(
            () => this.IncomingResponseReceived?.Invoke(response));
    }

    // ------------------------------------------------------------------
    // Incoming - Streams
    // ------------------------------------------------------------------

    public event Action<IncomingStreamOpened>? IncomingStreamOpened;
    public event Action<IncomingStreamData>? IncomingStreamData;
    public event Action<IncomingStreamClosed>? IncomingStreamClosed;
    public event Action<IncomingStreamAborted>? IncomingStreamAborted;

    public void PublishIncomingStreamOpened(IncomingStreamOpened streamOpened)
    {
        ArgumentNullException.ThrowIfNull(streamOpened);
        _queue.Writer.TryWrite(
            () => this.IncomingStreamOpened?.Invoke(streamOpened));
    }

    public void PublishIncomingStreamData(IncomingStreamData streamData)
    {
        ArgumentNullException.ThrowIfNull(streamData);
        _queue.Writer.TryWrite(
            () => this.IncomingStreamData?.Invoke(streamData));
    }

    public void PublishIncomingStreamClosed(IncomingStreamClosed streamClosed)
    {
        ArgumentNullException.ThrowIfNull(streamClosed);
        _queue.Writer.TryWrite(
            () => this.IncomingStreamClosed?.Invoke(streamClosed));
    }

    public void PublishIncomingStreamAborted(IncomingStreamAborted streamAborted)
    {
        ArgumentNullException.ThrowIfNull(streamAborted);
        _queue.Writer.TryWrite(
            () => this.IncomingStreamAborted?.Invoke(streamAborted));
    }
}
