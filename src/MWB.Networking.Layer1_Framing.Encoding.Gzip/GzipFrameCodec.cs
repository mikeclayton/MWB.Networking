using MWB.Networking.Layer1_Framing.Codec;
using MWB.Networking.Layer1_Framing.Codec.Abstractions;
using MWB.Networking.Layer1_Framing.Codec.Buffer;

namespace MWB.Networking.Layer1_Framing.Encoding.Gzip;

public sealed class GzipFrameCodec : IFrameCodec
{
    /// <summary>
    /// Decodes a complete value and forwards it unchanged.
    /// </summary>
    public FrameDecodeResult Decode(
        ICodecBufferReader inputReader,
        ICodecBufferWriter outputWriter)
    {
        GzipFrameCodec.CopyToWriter(inputReader, outputWriter);
        return FrameDecodeResult.Success;
    }

    /// <summary>
    /// Encodes a complete value and forwards it unchanged.
    /// </summary>
    public void Encode(
        ICodecBufferReader inputReader,
        ICodecBufferWriter outputWriter)
    {
        GzipFrameCodec.CopyToWriter(inputReader, outputWriter);
    }

    private static void CopyToWriter(
        ICodecBufferReader inputReader,
        ICodecBufferWriter outputWriter)
    {
        // Identity transform: copy value through unchanged
        while (inputReader.TryRead(out var memory))
        {
            outputWriter.Write(memory.Span);
            inputReader.Advance(memory.Length);
        }
    }
}
