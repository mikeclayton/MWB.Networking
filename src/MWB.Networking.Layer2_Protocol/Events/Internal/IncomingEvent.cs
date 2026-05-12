using MWB.Networking.Layer2_Protocol.Events.Api;

namespace MWB.Networking.Layer2_Protocol.Events.Internal;

/// <summary>
/// Represents a protocol event received from the remote peer.
///
/// This type is part of the internal protocol core and is used during inbound
/// event consumption to apply protocol-level semantics and coordinate publication.
/// It does not carry payload data and must not be exposed directly to application code.
/// </summary>
/// <remarks>
/// Incoming events are materialized into application-facing <see cref="Event"/>
/// instances at publication time, at which point the associated payload is attached.
///
/// A null event type has no special meaning to the protocol itself and does not
/// affect protocol behavior, validation, or routing.
/// </remarks>
internal sealed class IncomingEvent
{
    internal IncomingEvent(
        uint? eventType)
    {
        this.EventType = eventType;
    }

    /// <summary>
    /// Gets the optional application-defined event type.
    /// </summary>
    internal uint? EventType
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
