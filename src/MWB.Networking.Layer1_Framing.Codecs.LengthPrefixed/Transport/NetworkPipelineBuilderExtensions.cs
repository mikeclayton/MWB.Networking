using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Hosting;

namespace MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.Transport;

public static class NetworkPipelineBuilderExtensions
{
    public static INetworkPipelineBuildStage UseLengthPrefixedTransport(
        this INetworkPipelineCodecStage builder,
        ILogger logger,
        int maxFrameSize = 16 * 1024 * 1024)
    {
        return builder.UseTransportCodec(
            () => new LengthPrefixedTransportCodec(logger, maxFrameSize)
        );
    }
}
