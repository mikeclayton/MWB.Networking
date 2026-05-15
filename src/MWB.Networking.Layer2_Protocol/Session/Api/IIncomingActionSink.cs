using MWB.Networking.Layer2_Protocol.Events.Api;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Streams.Models;

namespace MWB.Networking.Layer2_Protocol.Session.Api;

internal interface IIncomingActionSink
{
    // Action sinks represent execution boundaries between the protocol layer
    // and the local application.
    //
    // All methods receive fully materialized domain objects that encapsulate
    // request, response, or event data along with their associated payload.
    //
    // These objects are immutable representations of protocol messages and
    // expose only application-relevant behaviour.

    // Events (remote peer → local application)
    void PublishIncomingEvent(IncomingEvent evt);

    // Requests (remote peer → local application)
    void PublishIncomingRequest(IncomingRequest request);

    // Responses (remote peer → local application)
    void PublishIncomingResponse(IncomingResponse response);

    void PublishIncomingStreamOpened(StreamOpenedMessage streamOpened);

    void PublishIncomingStreamData(StreamDataMessage streamData);

    void PublishIncomingStreamClosed(StreamClosedMessage streamClosed);

    void PublishIncomingStreamAborted(StreamAbortedMessage streamAborted);
}
