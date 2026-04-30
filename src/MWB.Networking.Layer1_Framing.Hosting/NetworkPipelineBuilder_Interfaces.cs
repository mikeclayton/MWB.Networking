using MWB.Networking.Layer1_Framing.Codec.Abstractions;
using MWB.Networking.Layer1_Framing.Pipeline;

namespace MWB.Networking.Layer1_Framing.Hosting;

// Stage 1: zero or more frame codecs
public interface INetworkPipelineCodecStage
{
    INetworkPipelineCodecStage UseFrameCodec(
        Func<IFrameCodec> frameCodecFactory);

    INetworkPipelineBuildStage UseTransportCodec(
        Func<ITransportCodec> transportFactory);
}

// Stage 2: must choose byte-stream codec
public interface INetworkPipelineBuildStage
{
    NetworkPipelineFactory BuildFactory();
}
