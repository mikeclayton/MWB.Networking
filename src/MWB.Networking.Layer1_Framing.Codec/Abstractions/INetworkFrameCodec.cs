using MWB.Networking.Layer1_Framing.Codec.Buffer;
using MWB.Networking.Layer1_Framing.Codec.Frames;

namespace MWB.Networking.Layer1_Framing.Codec.Abstractions;

/// <summary>
/// Codec defining the boundary between protocol semantics and framing.
/// Converts NetworkFrame instances to and from their binary form.
/// </summary>
public interface INetworkFrameCodec
{
    /// <summary>
    /// Encodes a single, complete input value into one or more output segments.
    /// This method is synchronous and must not block or await.
    /// </summary>
    void Encode(
        NetworkFrame inputFrame,
        ICodecBufferWriter outputWriter);

    /// <summary>
    /// Attempts to decode a single, complete output value from the provided bytes.
    /// Returns false if more data is required. On success, consumes input atomically
    /// and outputs exactly one decoded value.
    /// </summary>
    FrameDecodeResult Decode(
        ICodecBufferReader inputReader,
        out NetworkFrame outputFrame);
}
