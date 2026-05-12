using MWB.Networking.Layer2_Protocol.Events.Api;
using MWB.Networking.Layer2_Protocol.Requests.Api;

namespace MWB.Networking.Layer2_Protocol.Session.Api;

internal interface IOutgoingActionSink
{
    // Action sinks represent execution boundaries.
    // All methods receive fully materialized semantic domain objects.
    // Domain objects do not own payload lifetime.

    // Events (local application → remote peer)
    void TransmitOutgoingEvent(Event evt);

    // Requests (local application → remote peer)
    void TransmitOutgoingRequest(Request request);

    // Responses (local application → remote peer)
    void TransmitOutgoingResponse(Response response);
}
