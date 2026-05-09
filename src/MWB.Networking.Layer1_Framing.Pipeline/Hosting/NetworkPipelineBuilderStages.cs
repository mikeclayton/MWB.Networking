using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Codec.Abstractions;

namespace MWB.Networking.Layer1_Framing.Pipeline.Hosting;

public sealed class NetworkPipelineBuilderStages :
    INetworkPipelineBuilderLoggerStage,
    INetworkPipelineBuilderNetworkCodecStage,
    INetworkPipelineBuilderFrameCodecStage,
    INetworkPipelineBuilderBuildStage
{
    internal NetworkPipelineBuilderStages()
    {
    }

    // -----------------------------
    // Logger
    // -----------------------------

    private ILogger? _logger;

    public INetworkPipelineBuilderNetworkCodecStage UseLogger(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        return this;
    }

    // -----------------------------
    // Network codecs
    // -----------------------------

    private INetworkFrameCodec? _networkFrameCodec;

    INetworkPipelineBuilderFrameCodecStage INetworkPipelineBuilderNetworkCodecStage.UseNetworkFrameCodec(
        INetworkFrameCodec networkFrameCodec)
    {
        ArgumentNullException.ThrowIfNull(networkFrameCodec);

        _networkFrameCodec = networkFrameCodec;
        return this;
    }

    // -----------------------------
    // Frame codecs
    // -----------------------------

    private readonly List<IFrameCodec> _frameCodecs = [];

    INetworkPipelineBuilderFrameCodecStage INetworkPipelineBuilderFrameCodecStage.UseFrameCodec(
        IFrameCodec frameCodec)
    {
        ArgumentNullException.ThrowIfNull(frameCodec);

        _frameCodecs.Add(frameCodec);
        return this;
    }

    // -----------------------------
    // Terminal codec
    // -----------------------------

    private ITransportCodec? _transportCodec;

    INetworkPipelineBuilderBuildStage INetworkPipelineBuilderFrameCodecStage.UseTransportFrameCodec(
        ITransportCodec transportCodec)
    {
        ArgumentNullException.ThrowIfNull(transportCodec);

        _transportCodec = transportCodec;
        return this;
    }

    // -----------------------------
    // Build
    // -----------------------------

    NetworkPipeline INetworkPipelineBuilderBuildStage.Build()
    {
        var logger = _logger
            ?? throw new InvalidOperationException("A logger must be configured.");

        var networkFrameCodec = _networkFrameCodec
            ?? throw new InvalidOperationException("A network frame codec must be configured.");

        var transport = _transportCodec
            ?? throw new InvalidOperationException("A transport codec must be configured.");

        return new NetworkPipeline(
            logger,
            networkFrameCodec,
            _frameCodecs.AsReadOnly(),
            transport);
    }
}
