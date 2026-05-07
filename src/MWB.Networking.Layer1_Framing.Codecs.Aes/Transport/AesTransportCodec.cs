using MWB.Networking.Layer1_Framing.Codec;
using MWB.Networking.Layer1_Framing.Codec.Abstractions;
using MWB.Networking.Layer1_Framing.Codec.Buffer;
using System.Buffers;

namespace MWB.Networking.Layer1_Framing.Codecs.Aes.Transport;

public sealed class AesTransportCodec : ITransportCodec
{
    /// <summary>
    /// Attempts to decode a frame from the input byte stream.
    /// Treats all available bytes as a single complete frame.
    /// </summary>
    public bool TryDecode(
        ref ReadOnlySequence<byte> inputBytes,
        out ReadOnlyMemory<byte> frameBytes)
    {
        if (inputBytes.IsEmpty)
        {
            frameBytes = default;
            return false; // NeedsMoreData
        }

        // Materialise all available bytes as a single frame
        frameBytes = inputBytes.ToArray();

        // Consume everything
        inputBytes = inputBytes.Slice(inputBytes.End);

        return true;
    }

    /// <summary>
    /// Encodes a frame to the output stream unchanged.
    /// </summary>
    public void Encode(
        ICodecBufferReader inputReader,
        ICodecBufferWriter outputWriter)
    {
        // Identity transform: copy frame through unchanged
        while (inputReader.TryRead(out var memory))
        {
            outputWriter.Write(memory.Span);
            inputReader.Advance(memory.Length);
        }
    }
}
