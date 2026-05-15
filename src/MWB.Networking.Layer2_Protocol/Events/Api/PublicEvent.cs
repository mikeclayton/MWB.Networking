using MWB.Networking.Layer2_Protocol.Internal;

namespace MWB.Networking.Layer2_Protocol.Events.Api;

/// <summary>
/// Represents an event delivered to or emitted by the application.
/// </summary>
/// <remarks>
/// Encapsulates event metadata and payload associated with a protocol message.
/// Events are immutable data objects and do not participate in request lifecycle
/// or protocol invariant enforcement.
/// </remarks>
public abstract class PublicEvent
{
    private protected PublicEvent(
        uint? eventType,
        ReadOnlyMemory<byte> payload,
        ProtocolDirection direction)
    {
        this.EventType = eventType;
        this.Payload = payload;
        this.Direction = direction;
    }

    /// <summary>
    /// Gets the optional application-defined event type.
    /// </summary>
    public uint? EventType
    {
        get;
    }

    /// <summary>
    /// The payload associated with this event.
    /// </summary>
    public ReadOnlyMemory<byte> Payload
    {
        get;
    }

    private ProtocolDirection Direction
    {
        get;
    }
}
