using MWB.Networking.Layer1_Framing.Codec;
using MWB.Networking.Layer1_Framing.Codec.Abstractions;
using MWB.Networking.Layer1_Framing.Codec.Buffer;

namespace MWB.Networking.Layer1_Framing.Pipeline.UnitTests.Helpers;

/// <summary>
/// A test-double <see cref="IFrameCodec"/> whose <see cref="Decode"/> always
/// returns <see cref="FrameDecodeResult.InvalidFrameEncoding"/>.
/// </summary>
/// <remarks>
/// Used to verify pipeline atomicity: when a frame codec rejects the payload
/// the pipeline must not advance the transport sequence.
/// <see cref="Encode"/> is a no-op so that the pipeline can still produce wire
/// bytes that the transport will accept, allowing encode and decode paths to be
/// tested independently.
/// </remarks>
internal sealed class AlwaysFailFrameCodec : IFrameCodec
{
    /// <inheritdoc/>
    /// <remarks>No-op: passes input bytes through unchanged.</remarks>
    public void Encode(ICodecBufferReader inputReader, ICodecBufferWriter outputWriter)
    {
        // Intentionally pass through so encode round-trips can produce wire bytes.
        while (inputReader.TryRead(out var memory))
        {
            outputWriter.Write(memory.Span);
            inputReader.Advance(memory.Length);
        }
    }

    /// <inheritdoc/>
    /// <returns>Always <see cref="FrameDecodeResult.InvalidFrameEncoding"/>.</returns>
    public FrameDecodeResult Decode(ICodecBufferReader inputReader, ICodecBufferWriter outputWriter)
        => FrameDecodeResult.InvalidFrameEncoding;
}
