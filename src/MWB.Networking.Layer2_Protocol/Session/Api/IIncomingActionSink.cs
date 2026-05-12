using MWB.Networking.Layer2_Protocol.Events.Api;
using MWB.Networking.Layer2_Protocol.Requests.Api;

namespace MWB.Networking.Layer2_Protocol.Session.Api;

internal interface IIncomingActionSink
{
    // Action sinks represent execution boundaries.
    // All methods receive fully materialized semantic domain objects.
    // Domain objects do not own payload lifetime.

    // Events (remote peer → local application)
    void PublishIncomingEvent(Event evt);

    // Requests (remote peer → local application)
    void PublishIncomingRequest(Request request);

    // Responses (remote peer → local application)
    void PublishIncomingResponse(Response response);
}