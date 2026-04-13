namespace MWB.Networking.Layer2_Protocol.Internal;

internal static class ProtocolFrames
{
    // --------------------------------------------------------------
    // One-way events
    // --------------------------------------------------------------

    public static ProtocolFrame Event(uint eventType, ReadOnlyMemory<byte> payload)
        => new(ProtocolFrameKind.Event, eventType, null, null, payload);

    // --------------------------------------------------------------
    // Requests
    // --------------------------------------------------------------

    public static ProtocolFrame Request(uint requestId, ReadOnlyMemory<byte> payload)
        => new(ProtocolFrameKind.Request, null, requestId, null, payload);

    public static ProtocolFrame Response(uint requestId, ReadOnlyMemory<byte> payload)
        => new(ProtocolFrameKind.Response, null, requestId, null, payload);

    public static ProtocolFrame RequestError(uint requestId, ReadOnlyMemory<byte> payload)
        => new(ProtocolFrameKind.Error, null, requestId, null, payload);

    public static ProtocolFrame CompleteRequest(uint requestId)
        => new(ProtocolFrameKind.Complete, null, requestId, null, ReadOnlyMemory<byte>.Empty);

    public static ProtocolFrame CancelRequest(uint? requestId)
        => new(ProtocolFrameKind.Cancel, null, requestId, null, ReadOnlyMemory<byte>.Empty);


    // --------------------------------------------------------------
    // Streams
    // --------------------------------------------------------------

    public static ProtocolFrame StreamOpen(uint streamId, ReadOnlyMemory<byte> payload)
        => new(ProtocolFrameKind.StreamOpen, null, null, streamId, payload);

    public static ProtocolFrame StreamData(uint streamId, ReadOnlyMemory<byte> payload)
        => new(ProtocolFrameKind.StreamData, null, null, streamId, payload);

    public static ProtocolFrame StreamClose(uint streamId)
        => new(ProtocolFrameKind.StreamClose, null, null, streamId, ReadOnlyMemory<byte>.Empty);

    public static ProtocolFrame StreamError(uint streamId, ReadOnlyMemory<byte> payload)
        => new(ProtocolFrameKind.Error, null, null, streamId, payload);
}
