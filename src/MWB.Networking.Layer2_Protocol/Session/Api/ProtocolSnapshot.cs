namespace MWB.Networking.Layer2_Protocol.Session.Api;

internal sealed record ProtocolSnapshot(
    IReadOnlyCollection<uint> OpenRequests,
    IReadOnlyCollection<uint> OpenStreams);
