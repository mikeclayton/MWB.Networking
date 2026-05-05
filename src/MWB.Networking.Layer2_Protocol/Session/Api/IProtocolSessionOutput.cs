using MWB.Networking.Layer2_Protocol.Frames;

namespace MWB.Networking.Layer2_Protocol.Session.Api;

public interface IProtocolSessionOutput
{
    event Action<ProtocolFrame> OutboundFrameReady;
}
