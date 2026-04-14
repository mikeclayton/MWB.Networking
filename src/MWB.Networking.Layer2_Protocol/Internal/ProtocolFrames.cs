namespace MWB.Networking.Layer2_Protocol.Internal;

internal static class ProtocolFrames
{
    public static readonly ReadOnlyMemory<byte> EmptyPayload = ReadOnlyMemory<byte>.Empty;

    // --------------------------------------------------------------
    // One-way events
    // --------------------------------------------------------------

    public static ProtocolFrame Event(uint eventType)
        => new(ProtocolFrameKind.Event, eventType, null, null, ProtocolFrames.EmptyPayload);

    public static ProtocolFrame Event(uint eventType, ReadOnlyMemory<byte> payload)
        => new(ProtocolFrameKind.Event, eventType, null, null, payload);

    // --------------------------------------------------------------
    // Requests
    // --------------------------------------------------------------

    public static ProtocolFrame Request(uint requestId)
        => new(ProtocolFrameKind.Request, null, requestId, null, ProtocolFrames.EmptyPayload);

    public static ProtocolFrame Request(uint requestId, ReadOnlyMemory<byte> payload)
        => new(ProtocolFrameKind.Request, null, requestId, null, payload);

    public static ProtocolFrame Response(uint requestId)
        => new(ProtocolFrameKind.Response, null, requestId, null, ProtocolFrames.EmptyPayload);

    public static ProtocolFrame Response(uint requestId, ReadOnlyMemory<byte> payload)
        => new(ProtocolFrameKind.Response, null, requestId, null, payload);

    public static ProtocolFrame Error(uint requestId)
        => new(ProtocolFrameKind.Error, null, requestId, null, ProtocolFrames.EmptyPayload);

    public static ProtocolFrame Error(uint requestId, ReadOnlyMemory<byte> payload)
        => new(ProtocolFrameKind.Error, null, requestId, null, payload);

    // --------------------------------------------------------------
    // Streams
    // --------------------------------------------------------------

    /// <summary>
    /// Opens a request-scoped stream with an empty payload.
    /// </summary>
    public static ProtocolFrame StreamOpen(uint streamId, uint requestId)
        => new(ProtocolFrameKind.StreamOpen, null, requestId, streamId, ProtocolFrames.EmptyPayload);

    /// <summary>
    /// Opens a request-scoped stream with a payload.
    /// </summary>
    public static ProtocolFrame StreamOpen(uint streamId, uint requestId, ReadOnlyMemory<byte> payload)
        => new(ProtocolFrameKind.StreamOpen, null, requestId, streamId, payload);

    /// <summary>
    /// Opens a session-scoped stream with an empty payload.
    /// </summary>
    public static ProtocolFrame StreamOpen(uint streamId)
        => new(ProtocolFrameKind.StreamOpen, null, null, streamId, ProtocolFrames.EmptyPayload);

    /// <summary>
    /// Opens a session-scoped stream with a payload.
    /// </summary>
    public static ProtocolFrame StreamOpen(uint streamId, ReadOnlyMemory<byte> payload)
        => new(ProtocolFrameKind.StreamOpen, null, null, streamId, payload);

    public static ProtocolFrame StreamData(uint streamId, ReadOnlyMemory<byte> payload)
        => new(ProtocolFrameKind.StreamData, null, null, streamId, payload);

    public static ProtocolFrame StreamClose(uint streamId)
        => new(ProtocolFrameKind.StreamClose, null, null, streamId, ProtocolFrames.EmptyPayload);

    public static ProtocolFrame StreamAbort(uint streamId)
        => new(ProtocolFrameKind.StreamAbort, null, null, streamId, ProtocolFrames.EmptyPayload);

    public static ProtocolFrame StreamAbort(uint streamId, ReadOnlyMemory<byte> payload)
        => new(ProtocolFrameKind.StreamAbort, null, null, streamId, payload);
}
