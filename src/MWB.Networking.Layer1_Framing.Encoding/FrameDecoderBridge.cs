namespace MWB.Networking.Layer1_Framing.Encoding;

using global::MWB.Networking.Layer1_Framing.Encoding.Abstractions;
using System.Buffers;

/// <summary>
/// Bridges the inbound transport byte stream into the frame decoding pipeline.
///
/// This type performs no decoding itself; it exists solely as a boundary adapter
/// between Layer 0 (transport) and Layer 1 (framing), including lifecycle completion.
/// </summary>
public sealed class FrameDecoderBridge : IFrameDecoder
{
    private readonly IFrameDecoder _decoder;
    private readonly IFrameDecoderSink _sink;

    public FrameDecoderBridge(
        IFrameDecoder decoder,
        IFrameDecoderSink sink)
    {
        _decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
    }

    /// <summary>
    /// Entry point for inbound transport bytes.
    /// </summary>
    public ValueTask DecodeFrameAsync(
        ReadOnlySequence<byte> input,
        IFrameDecoderSink _,
        CancellationToken ct = default)
    {
        // Always use the internally-wired sink for the pipeline.
        return _decoder.DecodeFrameAsync(input, _sink, ct);
    }

    /// <summary>
    /// Signals end-of-stream to the decoding pipeline.
    /// </summary>
    public ValueTask CompleteAsync(
        IFrameDecoderSink _,
        CancellationToken ct = default)
    {
        // Delegate completion into the pipeline.
        return _decoder.CompleteAsync(_sink, ct);
    }
}