namespace MWB.Networking.Layer2_Protocol.Session.Frames;

/// <summary>
/// Represents a single protocol message exchanged between peers.
///
/// A <see cref="ProtocolFrame"/> is an atomic unit at the protocol layer and
/// may represent an Event, a Request, a Response, or Stream-related control
/// or data.
/// </summary>
public sealed class ProtocolFrame
{
    internal ProtocolFrame(
        ProtocolFrameKind kind,
        uint? eventType,
        uint? requestId,
        uint? requestType,
        uint? responseType,
        uint? streamId,
        uint? streamType,
        ReadOnlyMemory<byte> payload)
    {
        this.Kind = kind;
        this.EventType = eventType;
        this.RequestId = requestId;
        this.RequestType = requestType;
        this.ResponseType = responseType;
        this.StreamId = streamId;
        this.StreamType = streamType;
        this.Payload = payload;
    }

#if ENABLE_PROTOCOL_FRAME_DIAGNOSTICS

    private FrameDiagnostics _diagnostics;

    /// <summary>
    /// Optional frame-level diagnostics overlay.
    /// </summary>
    /// <remarks>
    /// Returned by <c>ref</c> so callers can mutate the underlying struct
    /// in-place without copying or allocating. This pattern is intentional
    /// and avoids per-frame overhead in hot paths.
    /// </remarks>
    public ref FrameDiagnostics Diagnostics => ref _diagnostics;

#endif

    /// <summary>
    /// The high-level kind of protocol message being transmitted.
    /// </summary>
    /// <remarks>
    /// The frame kind determines how the remaining fields should be interpreted
    /// and which protocol lifecycle rules apply.
    /// </remarks>
    public ProtocolFrameKind Kind
    {
        get;
    }

    /// <summary>
    /// Identifies the event type for Event frames.
    /// </summary>
    /// <remarks>
    /// This field is only populated for Event frames. Events are fire-and-forget
    /// notifications and are not associated with Requests or Streams.
    /// </remarks>
    public uint? EventType
    {
        get;
    }

    /// <remarks>
    /// For Request and Response frames, this field identifies the logical
    /// Request.
    ///
    /// A Request produces exactly one Response. Once a Response has been sent,
    /// the Request is considered complete and no further Request-scoped frames
    /// are permitted.
    ///
    /// Sending the Response implicitly completes and closes the Request-scoped
    /// Stream if one is associated with the Request.
    /// </remarks>
    public uint? RequestId
    {
        get;
    }

    public uint? RequestType
    {
        get;
    }

    public uint? ResponseType
    {
        get;
    }

    /// <summary>
    /// Identifies the Stream this frame belongs to.
    /// </summary>
    /// <remarks>
    /// Streams are directional and scoped by how they are opened.
    ///
    /// A stream may be:
    /// - <b>Request-scoped</b>, when opened as part of handling a specific
    ///   Request. In this case, at most one Request-scoped stream may be opened
    ///   per request, and it must be opened before the Response is sent.
    ///
    /// - <b>Session-scoped</b>, when opened independently of any Request.
    ///   session-scoped Streams are not tied to the lifecycle of Requests and
    ///   may be opened at any time while the session is active.
    ///
    /// Stream data flows in the direction implied by the Stream opening.
    /// Bidirectional communication can be modeled using multiple separate streams.
    /// </remarks>
    public uint? StreamId
    {
        get;
    }

    public uint? StreamType
    {
        get;
    }

    /// <summary>
    /// The frame payload.
    /// </summary>
    /// <remarks>
    /// The payload contains frame-specific data such as Event data, Request or
    /// Response metadata, or Stream data. Its interpretation depends on the
    /// <see cref="Kind"/> of the frame.
    /// </remarks>
    public ReadOnlyMemory<byte> Payload
    {
        get;
    }

    /// <summary>
    /// Creates a ProtocolFrame from raw decoded values.
    /// </summary>
    /// <remarks>
    /// This method performs no semantic validation and may create frames
    /// that violate protocol invariants.
    ///
    /// Intended for advanced infrastructure components such as protocol
    /// adapters, decoders, and tests.
    ///
    /// <para>
    /// Most callers should prefer the strongly-typed factory methods on
    /// <see cref="ProtocolFrames"/> (e.g. <c>Event</c>, <c>Request</c>,
    /// <c>StreamOpen</c>), which enforce protocol semantics.
    /// </para>
    /// </remarks>
    public static ProtocolFrame CreateRaw(
        ProtocolFrameKind kind,
        uint? eventType,
        uint? requestId,
        uint? requestType,
        uint? responseType,
        uint? streamId,
        uint? streamType,
        ReadOnlyMemory<byte> payload)
    {
        return new ProtocolFrame(
            kind,
            eventType,
            requestId,
            requestType,
            responseType,
            streamId,
            streamType,
            payload);
    }
}