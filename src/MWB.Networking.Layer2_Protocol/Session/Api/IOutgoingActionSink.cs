using MWB.Networking.Layer2_Protocol.Events.Api;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Streams.Publish;

namespace MWB.Networking.Layer2_Protocol.Session.Api;

internal interface IOutgoingActionSink
{
    // Action sinks represent execution boundaries between the local application
    // and the protocol layer.
    //
    // All methods receive fully materialized domain objects that encapsulate
    // request, response, or event data along with their associated payload.
    //
    // These objects are immutable representations of protocol messages and
    // expose only application-relevant behaviour.

    // Events (local application → remote peer)
    void TransmitOutgoingEvent(OutgoingEvent evt);

    // Requests (local application → remote peer)
    void TransmitOutgoingRequest(OutgoingRequest request);

    // Responses (local application → remote peer)
    void TransmitOutgoingResponse(OutgoingResponse response);

    void TransmitOutgoingStreamOpened(OutgoingStreamOpened streamOpened);

    void TransmitOutgoingStreamData(OutgoingStreamData streamData);

    void TransmitOutgoingStreamClosed(OutgoingStreamClosed streamClosed);

    void TransmitOutgoingStreamAborted(OutgoingStreamAborted streamAborted);
}
