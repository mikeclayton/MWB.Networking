namespace MWB.Networking.Layer1_Framing.Internal;

[Flags]
internal enum NetworkFrameFlags : byte
{
    None = 0,
    HasEventType = 1 << 0,
    HasRequestId = 1 << 1,
    HasRequestType = 1 << 2,
    HasStreamId = 1 << 3,
    HasStreamType = 1 << 4
}
