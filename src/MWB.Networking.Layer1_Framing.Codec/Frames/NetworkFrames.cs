namespace MWB.Networking.Layer1_Framing.Codec.Frames;

public static class NetworkFrames
{
    // ============================================================
    // Semantic factory methods (enforce invariants)
    // ============================================================

    // -----------------------------
    // Event
    // -----------------------------

    public static NetworkFrame Event(
        uint? eventType,
        ReadOnlyMemory<byte> payload = default)
    {
        return new NetworkFrame(
            kind: NetworkFrameKind.Event,
            eventType: eventType,
            requestId: null,
            requestType: null,
            responseType: null,
            streamId: null,
            streamType: null,
            payload: payload);
    }

    // -----------------------------
    // Request
    // -----------------------------

    public static NetworkFrame Request(
        uint requestId,
        uint? requestType = null,
        ReadOnlyMemory<byte> payload = default)
    {
        return new NetworkFrame(
            kind: NetworkFrameKind.Request,
            eventType: null,
            requestId: requestId,
            requestType: requestType,
            responseType: null,
            streamId: null,
            streamType: null,
            payload: payload);
    }

    // -----------------------------
    // Response
    // -----------------------------

    public static NetworkFrame Response(
        uint requestId,
        uint? responseType,
        ReadOnlyMemory<byte> payload)
    {
        return new NetworkFrame(
            kind: NetworkFrameKind.Response,
            eventType: null,
            requestId: requestId,
            requestType: null,
            responseType: responseType,
            streamId: null,
            streamType: null,
            payload: payload);
    }

    // -----------------------------
    // Stream Open
    // -----------------------------

    public static NetworkFrame StreamOpen(
        uint streamId,
        uint? streamType = null,
        uint? requestId = null,
        ReadOnlyMemory<byte> metadata = default)
    {
        return new NetworkFrame(
            kind: NetworkFrameKind.StreamOpen,
            eventType: null,
            requestId: requestId,
            requestType: null,
            responseType: null,
            streamId: streamId,
            streamType: streamType,
            payload: metadata);
    }

    // -----------------------------
    // Stream Data
    // -----------------------------

    public static NetworkFrame StreamData(
        uint streamId,
        ReadOnlyMemory<byte> payload = default)
    {
        return new NetworkFrame(
            kind: NetworkFrameKind.StreamData,
            eventType: null,
            requestId: null,
            requestType: null,
            responseType: null,
            streamId: streamId,
            streamType: null,
            payload: payload);
    }

    // -----------------------------
    // Stream Close
    // -----------------------------

    public static NetworkFrame StreamClose(
        uint streamId,
        ReadOnlyMemory<byte> metadata = default)
    {
        return new NetworkFrame(
            kind: NetworkFrameKind.StreamClose,
            eventType: null,
            requestId: null,
            requestType: null,
            responseType: null,
            streamId: streamId,
            streamType: null,
            payload: metadata);
    }
}
