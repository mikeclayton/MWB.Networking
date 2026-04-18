using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer1_Framing.Encoding.Abstractions;

namespace MWB.Networking.Layer1_Framing.Encoding.NullEncoder;

public sealed class NullFrameEncoder : IFrameEncoder
{
    public ValueTask EncodeFrameAsync(
        ByteSegments input,
        IFrameEncoderSink output,
        CancellationToken ct)
    {
         // intentionally emits nothing
        return ValueTask.CompletedTask;
    }
}