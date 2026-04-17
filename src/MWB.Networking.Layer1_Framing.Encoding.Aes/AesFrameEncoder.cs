using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer1_Framing.Encoding.Abstractions;

namespace MWB.Networking.Layer1_Framing.Encoding.Aes;

public sealed class AesFrameEncoder : IFrameEncoder
{  
    public ValueTask EncodeFrameAsync(
        ByteSegments input,
        IFrameEncoderSink output,
        CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
