using MWB.Networking.Layer2_Protocol.Events.Api;

namespace MWB.Networking.Layer2_Protocol.Events.Internal;


// <summary>
/// Represents a protocol event initiated by the local peer.
///
/// This type is part of the internal protocol core and exists to coordinate
/// outbound event semantics and transmission. It does not carry payload data
/// and must not be exposed directly to application code.
/// </summary>
/// <remarks>
/// Outgoing events are projected into application-facing <see cref="Event"/>
/// instances at transmission time. This type exists solely for protocol
/// coordination and invariant enforcement.
///
/// A null event type has no special meaning to the protocol itself and does not
/// affect protocol behavior, validation, or routing.
/// </remarks>
internal sealed class OutgoingEvent
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

    /// <summary>
    /// Projects this internal event into an application-facing <see cref="Event"/>,
    /// attaching the provided payload for publication or transmission. The
    /// publishable form must not be used internally for protocol-level processing
    /// or validation.
    /// </summary>
    internal Event AsPublishable(ReadOnlyMemory<byte> payload)
    {
        return new Event(
            this.EventType, payload
        );
    }
}
