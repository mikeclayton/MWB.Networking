using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Pipeline.Hosting;

namespace MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.Transport;

public static class NetworkPipelineBuilderExtensions
{
    public static INetworkPipelineBuildStage UseLengthPrefixedCodec(
        this INetworkPipelineFrameCodecStage builder,
        ILogger logger,
        int maxFrameSize = 16 * 1024 * 1024)
    {
        return builder.UseTransportFrameCodec(
            () => new LengthPrefixedTransportCodec(logger, maxFrameSize)
        );
    }
}
