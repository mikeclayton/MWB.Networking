namespace MWB.Networking.Layer1_Framing.Codec.Frames;

public sealed class NetworkFrame
{
    // ============================================================
    // Raw constructor (for decoding and low-level plumbing only)
    // ============================================================

    internal NetworkFrame(
        NetworkFrameKind kind,
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

    // ============================================================
    // Frame fields
    // ============================================================

    // Structural discriminator
    public NetworkFrameKind Kind
    {
        get;
    }

    // Application metadata (opaque at Layer 0)
    public uint? EventType
    {
        get;
    }

    // Correlation (opaque at Layer 0)
    public uint? RequestId
    {
        get;
    }

    // Application metadata (opaque at Layer 0)
    public uint? RequestType
    {
        get;
    }

    // Application metadata (opaque at Layer 0)
    public uint? ResponseType
    {
        get;
    }

    // Stream multiplexing (opaque at Layer 0)
    public uint? StreamId
    {
        get;
    }

    // Application metadata (opaque at Layer 0)
    public uint? StreamType
    {
        get;
    }

    // Opaque payload
    public ReadOnlyMemory<byte> Payload
    {
        get;
    }

    // ============================================================
    // Helpers
    // ============================================================

    /// <summary>
    /// Creates a NetworkFrame from raw decoded values.
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
    /// <see cref="NetworkFrames"/> (e.g. <c>Event</c>, <c>Request</c>,
    /// <c>StreamOpen</c>), which enforce protocol semantics.
    /// </para>
    /// </remarks>
    public static NetworkFrame CreateRaw(
        NetworkFrameKind kind,
        uint? eventType,
        uint? requestId,
        uint? requestType,
        uint? responseType,
        uint? streamId,
        uint? streamType,
        ReadOnlyMemory<byte> payload)
    {
        return new NetworkFrame(
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
