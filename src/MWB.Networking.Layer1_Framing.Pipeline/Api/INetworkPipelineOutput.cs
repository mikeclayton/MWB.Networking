using MWB.Networking.Layer1_Framing.Codec.Frames;

namespace MWB.Networking.Layer1_Framing.Pipeline.Api;

public interface INetworkPipelineOutput
{
    event Action<NetworkFrame> OutboundFrameReady;
}
