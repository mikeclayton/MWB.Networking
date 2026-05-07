using MWB.Networking.Layer1_Framing.Codec;
using MWB.Networking.Layer1_Framing.Codec.Abstractions;
using MWB.Networking.Layer1_Framing.Codec.Buffer;

namespace MWB.Networking.Layer1_Framing.Codecs.Reverse.Frame;

/// <summary>
/// A test-only <see cref="IFrameCodec"/> that reverses its input while
/// preserving the segment structure.
///
/// For multi-segment input each segment's bytes are individually reversed
/// <em>and</em> the segments themselves are emitted in reverse order, so that
/// the net logical effect on the concatenated byte stream is a simple reversal:
/// </summary>
/// <example>
/// Input  segments: [0x01, 0x02, 0x03] | [0x04, 0x05, 0x06]
/// Output segments: [0x06, 0x05, 0x04] | [0x03, 0x02, 0x01]
///
/// Concatenated input:  01 02 03 04 05 06
/// Concatenated output: 06 05 04 03 02 01   (exact reversal)
/// </example>
/// <remarks>
/// Because reversing is its own inverse, applying this codec twice — once for
/// encode and once for decode — returns the original bytes.  This makes it
/// useful for verifying that a pipeline stage correctly threads data through
/// both directions without corruption.
/// </remarks>
public sealed class ReverseFrameCodec : IFrameCodec
{
    /// <inheritdoc/>
    public void Encode(ICodecBufferReader inputReader, ICodecBufferWriter outputWriter)
        => Transform(inputReader, outputWriter);

    /// <inheritdoc/>
    public FrameDecodeResult Decode(ICodecBufferReader inputReader, ICodecBufferWriter outputWriter)
    {
        Transform(inputReader, outputWriter);
        return FrameDecodeResult.Success;
    }

    private static void Transform(ICodecBufferReader inputReader, ICodecBufferWriter outputWriter)
    {
        // Materialise all segments so they can be written in reverse order.
        var segments = new List<byte[]>();
        while (inputReader.TryRead(out var memory))
        {
            segments.Add(memory.ToArray());
            inputReader.Advance(memory.Length);
        }

        // Write in reverse segment order; reverse each segment's contents so
        // that the concatenated output is an exact reversal of the concatenated
        // input.
        for (var i = segments.Count - 1; i >= 0; i--)
        {
            var segment = segments[i];
            Array.Reverse(segment);
            outputWriter.Write(segment);
        }
    }
}
