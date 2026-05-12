namespace MWB.Networking.Layer2_Protocol.Events.Api;

/// <summary>
/// Represents an event delivered to or emitted by the application.
///
/// This is the application-facing projection of a protocol event and includes
/// both the event metadata and associated payload. Instances of this type are
/// materialized at publication or transmission time and do not participate in
/// protocol lifecycle or invariant enforcement.
/// </summary>
/// <remarks>
/// Events are fire-and-forget protocol signals and do not have a lifecycle,
/// identity beyond their delivery, or any acknowledgment semantics.
///
/// A null event type has no special meaning to the protocol itself and does not
/// affect protocol behavior, validation, or routing.
/// </remarks>
public sealed class Event
{
    internal Event(
        uint? eventType,
        ReadOnlyMemory<byte> payload)
    {
        this.EventType = eventType;
        this.Payload = payload;
    }

    /// <summary>
    /// Gets the optional application-defined event type.
    /// </summary>
    public uint? EventType
    {
        get;
    }

    public ReadOnlyMemory<byte> Payload
    {
        get;
    }
}
