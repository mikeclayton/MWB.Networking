using MWB.Networking.Layer2_Protocol.Frames;

namespace MWB.Networking.Layer2_Protocol.Session;

internal sealed partial class ProtocolSession
{
    internal void OnFrameReceived(ProtocolFrame frame)
    {
        this.AsProcessor().ProcessFrame(frame);
    }
}
