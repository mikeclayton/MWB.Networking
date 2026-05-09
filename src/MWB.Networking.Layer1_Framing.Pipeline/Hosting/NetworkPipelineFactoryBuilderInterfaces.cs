using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Codec.Abstractions;

namespace MWB.Networking.Layer1_Framing.Pipeline.Hosting;

/// <summary>
/// State 1 - Logger
/// </summary>
public interface INetworkPipelineFactoryBuilderLoggerStage
{
    INetworkPipelineFactoryBuilderNetworkCodecStage UseLogger(
        ILogger logger);
}

/// <summary>
/// Stage 2 - Network codec
/// </summary>
public interface INetworkPipelineFactoryBuilderNetworkCodecStage
{
    public INetworkPipelineFactoryBuilderFrameCodecStage UseNetworkFrameCodec(
      Func<INetworkFrameCodec> networkFrameCodecFactory);
}

/// <summary>
/// Stage 3 - Frame / Transport Codecs
/// </summary>
public interface INetworkPipelineFactoryBuilderFrameCodecStage
{
    INetworkPipelineFactoryBuilderFrameCodecStage UseFrameCodec(
        Func<IFrameCodec> frameCodecFactory);

    INetworkPipelineFactoryBuilderBuildStage UseTransportFrameCodec(
        Func<ITransportCodec> transportFactory);
}

/// <summary>
/// Stage 4 - Build
/// </summary>
public interface INetworkPipelineFactoryBuilderBuildStage
{
    NetworkPipelineFactory BuildFactory();
}
