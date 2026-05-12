namespace MWB.Networking.Layer2_Protocol.Events.Api;

/// <summary>
/// Represents a protocol event to be transmitted to the remote peer.
/// </summary>
/// <remarks>
/// A <see langword="null"/> <see cref="EventType"/> indicates that the event has
/// no application-defined type. The value is transmitted as-is to the remote peer
/// and interpreted according to application-defined semantics.
///
/// A null event type has no special meaning to the protocol itself and does not
/// affect protocol behavior, validation, or routing.
/// </remarks>
public sealed class OutgoingEvent
{
    internal OutgoingEvent(uint? eventType)
    {
        this.EventType = eventType;
    }

    /// <summary>
    /// Gets the optional application-defined event type.
    /// </summary>
    public uint? EventType
    {
        get;
    }
}
