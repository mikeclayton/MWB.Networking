using MWB.Networking.Layer1_Framing.Encoding.Abstractions;
using System.Buffers;

namespace MWB.Networking.Layer1_Framing.Encoding.NullEncoder;

public sealed class NullFrameDecoder : IFrameDecoder
{
    public ValueTask DecodeFrameAsync(
        ReadOnlySequence<byte> input,
        IFrameDecoderSink output,
        CancellationToken ct)
    {
        // intentionally emits nothing
        return ValueTask.CompletedTask;
    }

    public ValueTask CompleteAsync(
        IFrameDecoderSink output,
        CancellationToken ct = default)
    {
        // intentionally emits nothing
        return ValueTask.CompletedTask;
    }
}
