using MWB.Networking.Layer1_Framing.Codec.Buffer;
using System.Buffers;

namespace MWB.Networking.Layer1_Framing.Codec.Abstractions;

/// <summary>
/// Codec defining the boundary between framing and the underlying byte-stream transport.
/// </summary>
public interface ITransportCodec
{
    /// <summary>
    /// Encodes a single, complete input value into one or more output segments.
    /// This method is synchronous and must not block or await.
    /// </summary>
    /// <remarks>
    /// Must *not* be async.
    /// </remarks>
    void Encode(
        ICodecBufferReader inputReader,
        ICodecBufferWriter outputWwriter);

    /// <summary>
    /// Attempts to decode a single, complete output value from the provided bytes.
    /// Returns false if more data is required. On success, consumes input atomically
    /// and outputs exactly one decoded value.
    /// </summary>
    /// <remarks>
    /// Must *not* be async.
    /// </remarks>
    bool TryDecode(
        ref ReadOnlySequence<byte> inputBytes,
        out ReadOnlyMemory<byte> outputBytes);
}
