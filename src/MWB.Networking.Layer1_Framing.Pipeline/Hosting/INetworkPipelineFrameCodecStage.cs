using MWB.Networking.Layer1_Framing.Codec.Abstractions;

namespace MWB.Networking.Layer1_Framing.Pipeline.Hosting;

// Stage 1: zero or more frame codecs
public interface INetworkPipelineFrameCodecStage
{
    INetworkPipelineFrameCodecStage UseFrameCodec(
        Func<IFrameCodec> frameCodecFactory);

    INetworkPipelineBuildStage UseTransportFrameCodec(
        Func<ITransportCodec> transportFactory);
}
