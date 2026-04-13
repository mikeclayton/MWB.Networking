using MouseWithoutBorders.Networking.PeerTransport.Layer2_Protocol;

namespace MWB.Networking.Layer1_Framing;

public sealed class NetworkFrame
{
    public NetworkFrame(
        NetworkFrameKind kind,
        uint? eventType,
        uint? requestId = null,
        uint? streamId = null,
        uint? chunkIndex = null,
        bool isFinalChunk = false,
        ReadOnlyMemory<byte> payload = default)
    {
        this.Kind = kind;
        this.EventType = eventType;
        this.RequestId = requestId;
        this.StreamId = streamId;
        this.ChunkIndex = chunkIndex;
        this.IsFinalChunk = isFinalChunk;
        this.Payload = payload;
    }

    // Structural discriminator
    public NetworkFrameKind Kind
    {
        get;
    }

    // Correlation (opaque at Layer 0)
    public uint? EventType
    {
        get;
    }

    // Correlation (opaque at Layer 0)
    public uint? RequestId
    {
        get;
    }

    // Stream multiplexing (opaque at Layer 0)
    public uint? StreamId
    {
        get;
    }

    // Chunking / sequencing (structural only)
    public uint? ChunkIndex
    {
        get;
    }

    public bool IsFinalChunk
    {
        get;
    }

    // Opaque payload
    public ReadOnlyMemory<byte> Payload
    {
        get;
    }
}
