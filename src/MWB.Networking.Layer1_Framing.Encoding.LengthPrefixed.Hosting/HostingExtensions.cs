using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Hosting;

namespace MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed.Hosting;

public static class HostingExtensions
{
    public static INetworkPipelineBuildStage UseLengthPrefixedTransport(
            this INetworkPipelineCodecStage builder,
            ILogger logger,
            int maxFrameSize = 16 * 1024 * 1024)
    {
        return builder.UseTransportCodec(
            () => new LengthPrefixedFrameCodec(logger, maxFrameSize)
        );
    }
}
