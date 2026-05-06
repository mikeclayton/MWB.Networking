using MWB.Networking.Layer2_Protocol.Session.Frames;

namespace MWB.Networking.Layer2_Protocol.Session.Api;

public interface IProtocolSessionInput
{
    void OnFrameReceived(ProtocolFrame frame);
}
