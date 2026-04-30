using MWB.Networking.Layer1_Framing.Codec.Abstractions;
using MWB.Networking.Layer1_Framing.Pipeline;

namespace MWB.Networking.Layer1_Framing.Hosting;

// Stage 1: must choose NetworkFrameCodec
public interface INetworkPipelineStartStage
{
    INetworkPipelineCodecStage UseNetworkCodec(
        Func<INetworkFrameCodec> networkFrameCodecFactory);
}

// Stage 2: zero or more frame codecs
public interface INetworkPipelineCodecStage
{
    INetworkPipelineCodecStage UseFrameCodec(
        Func<IFrameCodec> frameCodecFactory);

    INetworkPipelineBuildStage UseTransportCodec(
        Func<ITransportCodec> transportFactory);
}

// Stage 3: must choose byte-stream codec
public interface INetworkPipelineBuildStage
{
    NetworkPipelineFactory BuildFactory();
}
