namespace MWB.Networking.Layer1_Framing;

[Flags]
internal enum NetworkFrameFlags : byte
{
    None = 0,
    HasEventType = 1 << 0,
    HasRequestId = 1 << 1,
    HasStreamId = 1 << 2,
    HasChunkIndex = 1 << 3,
    IsFinalChunk = 1 << 4,
}
