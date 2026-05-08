using MWB.Networking.Layer2_Protocol.Session.Frames;
using MWB.Networking.Layer2_Protocol.Session.Api;

namespace MWB.Networking.Layer2_Protocol.Session;

sealed partial class ProtocolSession : IProtocolSessionInput
{
    void IProtocolSessionInput.OnFrameReceived(ProtocolFrame frame)
    {
        this.AsProcessor().ProcessFrame(frame);
    }
}
