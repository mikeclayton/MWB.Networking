namespace MWB.Networking.Layer2_Protocol;

public sealed class ProtocolFrame
{
    public ProtocolFrame(
        ProtocolFrameKind kind,
        uint? eventType,
        uint? requestId,
        uint? streamId,
        ReadOnlyMemory<byte> payload)
    {
        this.Kind = kind;
        this.EventType = eventType;
        this.RequestId = requestId;
        this.StreamId = streamId;
        this.Payload = payload;
    }

    public ProtocolFrameKind Kind
    {
        get;
    }

    public uint? EventType
    {
        get;
    }

    /// <summary>
    /// Correlates frames belonging to a request lifecycle.
    /// Present for Request/Response/Complete/Cancel/Error.
    /// </summary>
    public uint? RequestId
    {
        get;
    }

    /// <summary>
    /// Correlates frames belonging to a stream lifecycle.
    /// Present for StreamOpen/StreamData/StreamClose/Cancel/Error.
    /// </summary>
    public uint? StreamId
    {
        get;
    }

    /// <summary>
    /// Opaque payload. Interpreted only by layers above Protocol.
    /// </summary>
    public ReadOnlyMemory<byte> Payload
    {
        get;
    }
}
