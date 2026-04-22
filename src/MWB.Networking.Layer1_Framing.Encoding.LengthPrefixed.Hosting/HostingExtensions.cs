using Microsoft.Extensions.Logging;
using MWB.Networking.Hosting;

namespace MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed.Hosting;

public static class HostingExtensions
{
    public static NetworkPipelineBuilder UseLengthPrefixedCodec(this NetworkPipelineBuilder builder, ILogger logger)
    {
        return builder.AppendFrameCodec(
            encoder: new LengthPrefixedFrameEncoder(logger),
            decoder: new LengthPrefixedFrameDecoder(logger)
        );
    }
}
