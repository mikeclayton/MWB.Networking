using MWB.Networking.Layer2_Protocol.Events.Api;
using MWB.Networking.Layer2_Protocol.Requests.Api;

namespace MWB.Networking.Layer2_Protocol.Session.Api;

internal interface IIncomingActionSink
{
    // Action sinks represent execution boundaries.
    // All methods receive a semantic domain object and an associated payload buffer.
    // Domain objects do not own payload lifetime.

    // Events (remote peer → local application)
    void PublishIncomingEvent(
        IncomingEvent evt,
        ReadOnlyMemory<byte> payload);

    // Requests (remote peer → local application)
    void PublishIncomingRequest(
        IncomingRequest request,
        ReadOnlyMemory<byte> payload);

    // Responses (remote peer → local application)
    void PublishIncomingResponse(
        IncomingResponse response,
        ReadOnlyMemory<byte> payload);
}