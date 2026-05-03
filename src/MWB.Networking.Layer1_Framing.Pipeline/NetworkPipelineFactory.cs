using MWB.Networking.Layer1_Framing.Codec.Abstractions;

namespace MWB.Networking.Layer1_Framing.Pipeline;

public sealed class NetworkPipelineFactory : INetworkPipelineFactory
{
    private readonly Func<INetworkFrameCodec> _networkFrameCodecFactory;
    private readonly IReadOnlyList<Func<IFrameCodec>> _frameCodecFactories;
    private readonly Func<ITransportCodec> _transportCodecFactory;

    public NetworkPipelineFactory(
        Func<INetworkFrameCodec> networkFrameCodecFactory,
        IReadOnlyList<Func<IFrameCodec>> frameCodecFactories,
        Func<ITransportCodec> transportCodecFactory)
    {
        _networkFrameCodecFactory =
            networkFrameCodecFactory ?? throw new ArgumentNullException(nameof(networkFrameCodecFactory));

        _frameCodecFactories =
            frameCodecFactories ?? throw new ArgumentNullException(nameof(frameCodecFactories));

        _transportCodecFactory =
            transportCodecFactory ?? throw new ArgumentNullException(nameof(transportCodecFactory));
    }

    public NetworkPipeline CreatePipeline()
    {
        // Create fresh codec instances for this pipeline
        var networkFrameCodec = _networkFrameCodecFactory();

        var frameCodecs = _frameCodecFactories
            .Select(f => f())
            .ToList()
            .AsReadOnly();

        var transportCodec = _transportCodecFactory();

        return new NetworkPipeline(
            networkFrameCodec,
            frameCodecs,
            transportCodec);
    }
}
