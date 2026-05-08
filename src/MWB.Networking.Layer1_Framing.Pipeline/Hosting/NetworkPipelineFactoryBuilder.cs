using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Codec.Abstractions;

namespace MWB.Networking.Layer1_Framing.Pipeline.Hosting;

public sealed class NetworkPipelineFactoryBuilder :
    INetworkPipelineFactoryBuilderLoggerStage,
    INetworkPipelineFactoryBuilderNetworkCodecStage,
    INetworkPipelineFactoryBuilderFrameCodecStage,
    INetworkPipelineFactoryBuilderBuildStage
{
    internal NetworkPipelineFactoryBuilder()
    {
    }

    public static INetworkPipelineFactoryBuilderLoggerStage Create()
    {
        return new NetworkPipelineFactoryBuilder();
    }

    // -----------------------------
    // Logger
    // -----------------------------

    private ILogger? _logger;

    public INetworkPipelineFactoryBuilderNetworkCodecStage UseLogger(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        return this;
    }

    // -----------------------------
    // Network codecs
    // -----------------------------

    private Func<INetworkFrameCodec>? _networkFrameCodecFactory;

    INetworkPipelineFactoryBuilderFrameCodecStage INetworkPipelineFactoryBuilderNetworkCodecStage.UseNetworkFrameCodec(
        Func<INetworkFrameCodec> networkFrameCodecFactory)
    {
        ArgumentNullException.ThrowIfNull(networkFrameCodecFactory);

        _networkFrameCodecFactory = networkFrameCodecFactory;
        return this;
    }

    // -----------------------------
    // Frame codecs
    // -----------------------------

    private readonly List<Func<IFrameCodec>> _frameCodecFactories = [];

    INetworkPipelineFactoryBuilderFrameCodecStage INetworkPipelineFactoryBuilderFrameCodecStage.UseFrameCodec(
        Func<IFrameCodec> frameCodecFactory)
    {
        ArgumentNullException.ThrowIfNull(frameCodecFactory);

        _frameCodecFactories.Add(frameCodecFactory);
        return this;
    }

    // -----------------------------
    // Terminal codec
    // -----------------------------

    private Func<ITransportCodec>? _transportCodecFactory;

    INetworkPipelineFactoryBuilderBuildStage INetworkPipelineFactoryBuilderFrameCodecStage.UseTransportFrameCodec(
        Func<ITransportCodec> transportCodecFactory)
    {
        ArgumentNullException.ThrowIfNull(transportCodecFactory);

        _transportCodecFactory = transportCodecFactory;
        return this;
    }

    // -----------------------------
    // Build
    // -----------------------------

    NetworkPipelineFactory INetworkPipelineFactoryBuilderBuildStage.BuildFactory()
    {
        var logger = _logger
            ?? throw new InvalidOperationException("A logger must be configured.");

        var networkFrameCodecFactory = _networkFrameCodecFactory
            ?? throw new InvalidOperationException("A network frame codec must be configured.");

        var transportFactory = _transportCodecFactory
            ?? throw new InvalidOperationException("A transport codec must be configured.");

        return new NetworkPipelineFactory(
            logger,
            networkFrameCodecFactory,
            _frameCodecFactories.AsReadOnly(),
            transportFactory);
    }
}
