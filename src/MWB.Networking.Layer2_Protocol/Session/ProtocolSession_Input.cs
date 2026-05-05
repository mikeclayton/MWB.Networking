using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Session.Api;

namespace MWB.Networking.Layer2_Protocol.Session;

public sealed partial class ProtocolSession : IProtocolSessionInput
{
    void IProtocolSessionInput.OnFrameReceived(ProtocolFrame frame)
    {
        this.AsProcessor().ProcessFrame(frame);
    }
}
