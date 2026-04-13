namespace MWB.Networking.Layer2_Protocol;

public sealed record ProtocolSnapshot(
    IReadOnlyCollection<uint> OpenRequests,
    IReadOnlyCollection<uint> OpenStreams);
