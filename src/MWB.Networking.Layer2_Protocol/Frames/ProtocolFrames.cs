namespace MWB.Networking.Layer2_Protocol.Frames;

internal static class ProtocolFrames
{
    // ============================================================
    // Semantic factory methods (enforce invariants)
    // ============================================================

    // -----------------------------
    // Event
    // -----------------------------

    public static ProtocolFrame Event(
        uint? eventType = null,
        ReadOnlyMemory<byte> payload = default)
    {
        return new ProtocolFrame(
            kind: ProtocolFrameKind.Event,
            eventType: eventType,
            requestId: null,
            requestType: null,
            streamId: null,
            streamType: null,
            payload: payload);
    }

    // -----------------------------
    // Request
    // -----------------------------

    public static ProtocolFrame Request(
        uint requestId,
        uint? requestType = null,
        ReadOnlyMemory<byte> payload = default)
    {
        return new ProtocolFrame(
            kind: ProtocolFrameKind.Request,
            eventType: null,
            requestId: requestId,
            requestType: requestType,
            streamId: null,
            streamType: null,
            payload: payload);
    }

    // -----------------------------
    // Response
    // -----------------------------

    public static ProtocolFrame Response(
        uint requestId,
        ReadOnlyMemory<byte> payload = default)
    {
        return new ProtocolFrame(
            kind: ProtocolFrameKind.Response,
            eventType: null,
            requestId: requestId,
            requestType: null,
            streamId: null,
            streamType: null,
            payload: payload);
    }

    // -----------------------------
    // Error
    // -----------------------------

    public static ProtocolFrame Error(uint requestId, ReadOnlyMemory<byte> payload = default)
    {
        return new ProtocolFrame(
            kind: ProtocolFrameKind.Error,
            eventType: null,
            requestId: requestId,
            requestType: null,
            streamId: null,
            streamType: null,
            payload: payload);
    }

    // -----------------------------
    // Stream Open
    // -----------------------------

    public static ProtocolFrame StreamOpen(
        uint streamId,
        uint? streamType = null,
        uint? requestId = null,
        ReadOnlyMemory<byte> metadata = default)
    {
        return new ProtocolFrame(
            kind: ProtocolFrameKind.StreamOpen,
            eventType: null,
            requestId: requestId,
            requestType: null,
            streamId: streamId,
            streamType: streamType,
            payload: metadata);
    }

    // -----------------------------
    // Stream Data
    // -----------------------------

    public static ProtocolFrame StreamData(
        uint streamId,
        ReadOnlyMemory<byte> payload = default)
    {
        return new ProtocolFrame(
            kind: ProtocolFrameKind.StreamData,
            eventType: null,
            requestId: null,
            requestType: null,
            streamId: streamId,
            streamType: null,
            payload: payload);
    }

    // -----------------------------
    // Stream Close
    // -----------------------------

    public static ProtocolFrame StreamClose(
        uint streamId,
        ReadOnlyMemory<byte> metadata = default)
    {
        return new ProtocolFrame(
            kind: ProtocolFrameKind.StreamClose,
            eventType: null,
            requestId: null,
            requestType: null,
            streamId: streamId,
            streamType: null,
            payload: metadata);
    }

    // -----------------------------
    // Stream Abort
    // -----------------------------

    public static ProtocolFrame StreamAbort(
        uint streamId,
        ReadOnlyMemory<byte> metadata = default)
    {
        return new ProtocolFrame(
            kind: ProtocolFrameKind.StreamClose,
            eventType: null,
            requestId: null,
            requestType: null,
            streamId: streamId,
            streamType: null,
            payload: metadata);
    }
}
