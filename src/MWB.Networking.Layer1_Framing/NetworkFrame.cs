namespace MWB.Networking.Layer1_Framing;

public sealed class NetworkFrame
{
    // ============================================================
    // Raw constructor (for decoding and low-level plumbing only)
    // ============================================================

    public NetworkFrame(
        NetworkFrameKind kind,
        uint? eventType,
        uint? requestId,
        uint? requestType,
        uint? streamId,
        uint? streamType,
        ReadOnlyMemory<byte> payload)
    {
        this.Kind = kind;
        this.EventType = eventType;
        this.RequestId = requestId;
        this.RequestType = requestType;
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
}
