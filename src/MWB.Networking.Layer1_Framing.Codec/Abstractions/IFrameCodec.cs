using MWB.Networking.Layer1_Framing.Codec.Buffer;
using System.Buffers;

namespace MWB.Networking.Layer1_Framing.Codec.Abstractions;

/// <summary>
/// Framing codec that performs structural transformations over framed binary data.
/// These codecs operate over segmented pipeline input and emit new pipeline segments.
/// </summary>
public interface IFrameCodec
{
    void Encode(
        ICodecBufferReader inputReader,
        ICodecBufferWriter outputWriter);

    FrameDecodeResult Decode(
        ICodecBufferReader inputReader,
        ICodecBufferWriter outputWriter);
}
