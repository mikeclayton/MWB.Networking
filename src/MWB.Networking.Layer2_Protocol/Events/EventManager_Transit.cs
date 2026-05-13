using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Events.Api;

namespace MWB.Networking.Layer2_Protocol.Events;

/// <summary>
/// Contains ingress and egress methods for events transiting
/// between the session and the external boundary.
/// </summary>
internal sealed partial class EventManager
{
    // ------------------------------------------------------------
    // Incoming Event ingress
    // (driver → adapter → session → adapter → application)
    //  ^^^^^^^^^^^^^^^^^^^^^^^^^^
    // ------------------------------------------------------------

    /// <summary>
    /// Consumes an incoming protocol event from a remote peer.
    /// </summary>
    /// <remarks>
    /// This method represents the ingress boundary for inbound events. It
    /// takes ownership of the incoming event data, applies event-level
    /// protocol semantics, and constructs the corresponding <see cref="IncomingEvent"/>
    /// representation.
    ///
    /// After semantic processing, the event is surfaced beyond the protocol
    /// boundary via the session’s incoming action sink. Delivery, scheduling,
    /// and transport concerns are handled downstream by the session adapter.
    ///
    /// The caller should treat the input as consumed regardless of outcome;
    /// validation, filtering, or rejection are internal protocol concerns.
    /// </remarks>
    internal IncomingEvent ConsumeIncomingEvent(
        uint? eventType,
        ReadOnlyMemory<byte> payload)
    {
        this.Logger.LogTrace(
            "Consuming incoming event (Type={EventType})",
            eventType);

        var incomingEvent = new IncomingEvent(eventType, payload);
        this.PublishIncomingEvent(incomingEvent);
        return incomingEvent;
    }

    // ------------------------------------------------------------
    // Incoming Event egress
    // (driver → adapter → session → adapter → application)
    //                     ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
    // ------------------------------------------------------------

    /// <summary>
    /// Publishes an incoming event for delivery to the local application.
    /// </summary>
    /// <remarks>
    /// This method represents the inbound egress boundary of the protocol. It is
    /// called after event-level semantics have been applied and emits the resulting
    /// <see cref="IncomingEvent"/> to the session’s incoming action sink, marking the
    /// point at which the event is surfaced beyond the protocol boundary to the
    /// local, in-process application.
    ///
    /// Actual delivery mechanics, adapter concerns, and dispatch to application
    /// handlers are managed downstream by the session adapter.
    /// </remarks>
    internal void PublishIncomingEvent(IncomingEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

        this.Logger.LogTrace(
            "Publishing incoming event (Type={EventType})",
            evt.EventType);

        this.Session.IncomingActionSink.PublishIncomingEvent(evt);
    }

    // ------------------------------------------------------------
    // Outgoing Event ingress
    // (application → adapter → session → adapter → driver)
    //  ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
    // ------------------------------------------------------------

    /// <summary>
    /// Consumes a locally generated outgoing event.
    /// </summary>
    /// <remarks>
    /// This method represents the ingress boundary for outbound events originating
    /// within the local peer. It takes ownership of the outgoing event data, applies
    /// event-level protocol semantics, and constructs the corresponding
    /// <see cref="OutgoingEvent"/> representation.
    ///
    /// After semantic processing, the event is surfaced to the session’s outgoing
    /// action sink for execution. Ordering, transport, and delivery concerns are
    /// handled downstream by the session adapter.
    ///
    /// The caller should treat the input as consumed regardless of outcome;
    /// validation, filtering, or rejection are internal protocol concerns.
    /// </remarks>
    internal OutgoingEvent ConsumeOutgoingEvent(
        uint? eventType,
        ReadOnlyMemory<byte> payload)
    {
        this.Logger.LogTrace(
            "Consuming outgoing event (Type={EventType})",
            eventType);

        var outgoingEvent = new OutgoingEvent(eventType, payload);
        this.TransmitOutgoingEvent(outgoingEvent);
        return outgoingEvent;
    }

    // ------------------------------------------------------------
    // Outgoing Event egress
    // (application → adapter → session → adapter → driver)
    //                          ^^^^^^^^^^^^^^^^^^^^^^^^^^
    // ------------------------------------------------------------

    /// <summary>
    /// Transmits an outgoing event for delivery to the remote peer.
    /// </summary>
    /// <remarks>
    /// This method represents the outbound protocol boundary for events originating
    /// within the local peer. It takes ownership of the outgoing event description,
    /// applies any event-level protocol semantics, commits the resulting event
    /// for delivery to the remote peer.
    ///
    /// Actual execution concerns such as scheduling, ordering, transport, and
    /// delivery are handled downstream by the session’s outgoing action sink
    /// and adapter.
    /// </remarks>
    internal void TransmitOutgoingEvent(OutgoingEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

        this.Logger.LogTrace(
            "Transmitting outgoing event (Type={EventType})",
            evt.EventType);

        this.Session.OutgoingActionSink.TransmitOutgoingEvent(evt);
    }
}
