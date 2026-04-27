using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Hosting;

namespace MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed.Hosting;

public static class HostingExtensions
{
    public static NetworkPipelineBuilder UseLengthPrefixedCodec(this NetworkPipelineBuilder factory, ILogger logger)
    {
        return factory.AppendFrameCodec(
            encoder: new LengthPrefixedFrameEncoder(logger),
            decoder: new LengthPrefixedFrameDecoder(logger)
        );
    }
}
