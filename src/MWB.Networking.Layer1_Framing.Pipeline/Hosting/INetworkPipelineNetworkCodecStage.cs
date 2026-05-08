using MWB.Networking.Layer1_Framing.Codec.Abstractions;

namespace MWB.Networking.Layer1_Framing.Pipeline.Hosting;

// Stage 1: zero or more frame codecs
public interface INetworkPipelineNetworkCodecStage
{
    public INetworkPipelineFrameCodecStage UseNetworkFrameCodec(
      Func<INetworkFrameCodec> networkFrameCodecFactory);
}
