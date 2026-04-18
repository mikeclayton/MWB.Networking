using MWB.Networking.Layer1_Framing.Encoding.Abstractions;
using System.Buffers;

namespace MWB.Networking.Layer1_Framing.Encoding.Aes;

public sealed class AesFrameDecoder : IFrameDecoder
{
    public ValueTask DecodeFrameAsync(
        ReadOnlySequence<byte> input,
        IFrameDecoderSink output,
        CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public ValueTask CompleteAsync(
        IFrameDecoderSink output,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
