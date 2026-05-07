using MWB.Networking.Layer1_Framing.Codec.Frames;

namespace MWB.Networking.Layer2_Protocol.Adapter.UnitTests;

/// <summary>
/// Convenience factory for creating <see cref="NetworkFrame"/> instances in tests
/// without going through the full <see cref="NetworkFrames"/> semantic factories.
/// Uses <see cref="NetworkFrame.CreateRaw"/> so every field can be set explicitly.
/// </summary>
internal static class NetworkFrameFactory
{
    public static NetworkFrame Event(
        uint? eventType = null,
        byte[]? payload = null)
        => NetworkFrame.CreateRaw(
            NetworkFrameKind.Event,
            eventType, null, null, null, null, null,
            payload is not null ? new ReadOnlyMemory<byte>(payload) : default);

    public static NetworkFrame Request(
        uint requestId,
        uint? requestType = null,
        byte[]? payload = null)
        => NetworkFrame.CreateRaw(
            NetworkFrameKind.Request,
            null, requestId, requestType, null, null, null,
            payload is not null ? new ReadOnlyMemory<byte>(payload) : default);

    public static NetworkFrame Response(
        uint requestId,
        uint? responseType = null,
        byte[]? payload = null)
        => NetworkFrame.CreateRaw(
            NetworkFrameKind.Response,
            null, requestId, null, responseType, null, null,
            payload is not null ? new ReadOnlyMemory<byte>(payload) : default);

    public static NetworkFrame Error(
        uint requestId,
        byte[]? payload = null)
        => NetworkFrame.CreateRaw(
            NetworkFrameKind.Error,
            null, requestId, null, null, null, null,
            payload is not null ? new ReadOnlyMemory<byte>(payload) : default);

    public static NetworkFrame StreamOpen(
        uint streamId,
        uint? streamType = null,
        uint? requestId = null)
        => NetworkFrame.CreateRaw(
            NetworkFrameKind.StreamOpen,
            null, requestId, null, null, streamId, streamType, default);

    public static NetworkFrame StreamData(
        uint streamId,
        byte[]? payload = null)
        => NetworkFrame.CreateRaw(
            NetworkFrameKind.StreamData,
            null, null, null, null, streamId, null,
            payload is not null ? new ReadOnlyMemory<byte>(payload) : default);

    public static NetworkFrame StreamClose(uint streamId)
        => NetworkFrame.CreateRaw(
            NetworkFrameKind.StreamClose,
            null, null, null, null, streamId, null, default);

    public static NetworkFrame StreamAbort(uint streamId)
        => NetworkFrame.CreateRaw(
            NetworkFrameKind.StreamAbort,
            null, null, null, null, streamId, null, default);
}
