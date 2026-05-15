using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Codec.Abstractions;

namespace MWB.Networking.Layer1_Framing.Pipeline.Hosting;

/// <summary>
/// State 1 - Logger
/// </summary>
public interface INetworkPipelineBuilderLoggerStage
{
    INetworkPipelineBuilderNetworkCodecStage UseLogger(
        ILogger logger);
}

/// <summary>
/// Stage 2 - Network codec
/// </summary>
public interface INetworkPipelineBuilderNetworkCodecStage
{
    public INetworkPipelineBuilderFrameCodecStage UseNetworkFrameCodec(
      INetworkFrameCodec networkFrameCodec);
}

/// <summary>
/// Stage 3 - Frame / Transport Codecs
/// </summary>
public interface INetworkPipelineBuilderFrameCodecStage
{
    INetworkPipelineBuilderFrameCodecStage UseFrameCodec(
        IFrameCodec frameCodec);

    INetworkPipelineBuilderBuildStage UseTransportFrameCodec(
        ITransportCodec transport);
}

/// <summary>
/// Stage 4 - Build
/// </summary>
public interface INetworkPipelineBuilderBuildStage
{
    NetworkPipeline Build();
}
