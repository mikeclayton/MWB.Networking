using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Encoding.NullEncoder;
using MWB.Networking.Layer1_Framing.Hosting;

namespace MWB.Networking.Layer1_Framing.NullEncoder.Hosting;

public static class HostingExtensions
{
    public static NetworkPipelineBuilder UseNullCodec(this NetworkPipelineBuilder factory)
    {
        return factory.AppendFrameCodec(
            encoder: new NullFrameEncoder(),
            decoder: new NullFrameDecoder()
        );
    }
}
