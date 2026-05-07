using MWB.Networking.Layer1_Framing.Codec.Abstractions;

namespace MWB.Networking.Layer1_Framing.Pipeline.Hosting
{
    internal sealed class NetworkPipelineBuilderState :
        INetworkPipelineCodecStage,
        INetworkPipelineBuildStage
    {
        internal NetworkPipelineBuilderState()
        {
        }

        // -----------------------------
        // Initial codecs
        // -----------------------------

        private Func<INetworkFrameCodec>? _networkFrameCodecFactory;

        internal NetworkPipelineBuilderState UseNetworkCodec(
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

        INetworkPipelineCodecStage INetworkPipelineCodecStage.UseFrameCodec(
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

        INetworkPipelineBuildStage INetworkPipelineCodecStage.UseTransportCodec(
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
