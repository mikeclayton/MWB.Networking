using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Codec.Abstractions;

namespace MWB.Networking.Layer1_Framing.Pipeline.Hosting
{
    internal sealed class NetworkPipelineBuilderState :
        INetworkPipelineNetworkCodecStage,
        INetworkPipelineFrameCodecStage,
        INetworkPipelineBuildStage
    {
        internal NetworkPipelineBuilderState()
        {
        }

        // -----------------------------
        // Logger
        // -----------------------------

        private ILogger? _logger;

        internal INetworkPipelineNetworkCodecStage UseLogger(ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            _logger = logger;
            return this;
        }

        // -----------------------------
        // Network codecs
        // -----------------------------

        private Func<INetworkFrameCodec>? _networkFrameCodecFactory;

        INetworkPipelineFrameCodecStage INetworkPipelineNetworkCodecStage.UseNetworkFrameCodec(
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

        INetworkPipelineFrameCodecStage INetworkPipelineFrameCodecStage.UseFrameCodec(
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

        INetworkPipelineBuildStage INetworkPipelineFrameCodecStage.UseTransportFrameCodec(
            Func<ITransportCodec> transportCodecFactory)
        {
            ArgumentNullException.ThrowIfNull(transportCodecFactory);

            _transportCodecFactory = transportCodecFactory;
            return this;
        }

        // -----------------------------
        // Build
        // -----------------------------

        public NetworkPipelineFactory BuildFactory()
        {
            var networkFrameCodecFactory = _networkFrameCodecFactory
                ?? throw new InvalidOperationException("A network frame codec must be configured.");

            var transportFactory  = _transportCodecFactory
                ?? throw new InvalidOperationException("A transport codec must be configured.");

            return new NetworkPipelineFactory(
                networkFrameCodecFactory,
                _frameCodecFactories.AsReadOnly(),
                transportFactory);
        }
    }
}
