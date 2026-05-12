using MWB.Networking.Layer2_Protocol.Events.Api;
using MWB.Networking.Layer2_Protocol.Requests.Api;

namespace MWB.Networking.Layer2_Protocol.Session.Api;

internal interface IOutgoingActionSink
{
    // Action sinks represent execution boundaries.
    // All methods receive a semantic domain object and an associated payload buffer.
    // Domain objects do not own payload lifetime.

    // Events (local application → remote peer)
    void TransmitOutgoingEvent(
        OutgoingEvent evt,
        ReadOnlyMemory<byte> payload);

    // Requests (local application → remote peer)
    void TransmitOutgoingRequest(
        OutgoingRequest request,
        ReadOnlyMemory<byte> payload);

    // Responses (local application → remote peer)
    void TransmitOutgoingResponse(
        OutgoingResponse response,
        ReadOnlyMemory<byte> payload);
}
