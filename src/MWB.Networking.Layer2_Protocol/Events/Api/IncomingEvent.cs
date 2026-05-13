using MWB.Networking.Layer2_Protocol.Internal;

namespace MWB.Networking.Layer2_Protocol.Events.Api;

/// <summary>
/// Represents an event received from a remote peer.
/// </summary>
/// <remarks>
/// Delivered to the application when a remote peer emits an event.
/// </remarks>
public sealed class IncomingEvent : PublicEvent
{
    internal IncomingEvent(
        uint? eventType,
        ReadOnlyMemory<byte> payload)
        : base(eventType, payload, ProtocolDirection.Incoming)
    {
    }
}
