using MWB.Networking.Layer2_Protocol.Internal;

namespace MWB.Networking.Layer2_Protocol.Events.Api;

/// <summary>
/// Represents an event sent to a remote peer.
/// </summary>
/// <remarks>
/// Created by the application to transmit an event to a remote peer.
/// </remarks>
public sealed class OutgoingEvent : PublicEvent
{
    internal OutgoingEvent(
        uint? eventType,
        ReadOnlyMemory<byte> payload)
        : base(eventType, payload, ProtocolDirection.Outgoing)
    {
    }
}
