using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer1_Framing.Encoding.Abstractions;
using System.Buffers;

namespace MWB.Networking.Layer1_Framing.Encoding;

internal sealed class PipelineDecoderSink : IFrameDecoderSink
{
    private readonly IFrameDecoder _decoder;
    private readonly IFrameDecoderSink _next;

    public PipelineDecoderSink(
        IFrameDecoder decoder,
        IFrameDecoderSink next)
    {
        _decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    /// <summary>
    /// Entry point for byte input from the transport.
    /// </summary>
    public ValueTask DecodeFrameAsync(
        ReadOnlySequence<byte> input,
        CancellationToken ct)
    {
        // Delegate decoding to the wrapped decoder,
        // passing ourselves as the sink.
        return _decoder.DecodeFrameAsync(input, this, ct);
    }

    /// <summary>
    /// Receives decoded frames from the wrapped decoder
    /// and forwards them downstream.
    /// </summary>
    public ValueTask OnFrameDecodedAsync(
        ByteSegments frame,
        CancellationToken ct)
    {
        return _next.OnFrameDecodedAsync(frame, ct);
    }

    public ValueTask CompleteAsync(CancellationToken ct)
    {
        return _decoder.CompleteAsync(_next, ct);
    }
}
