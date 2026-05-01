namespace MWB.Networking.Layer1_Framing.Codecs.Default.Network;

[Flags]
internal enum NetworkFrameFlags : byte
{
    None = 0,
    HasEventType = 1 << 0,
    HasRequestId = 1 << 1,
    HasRequestType = 1 << 2,
    HasResponseType = 1 << 3,
    HasStreamId = 1 << 4,
    HasStreamType = 1 << 5,

    // Reserved for future structural extensions
    Reserved6 = 1 << 6,
    Reserved7 = 1 << 7
}
