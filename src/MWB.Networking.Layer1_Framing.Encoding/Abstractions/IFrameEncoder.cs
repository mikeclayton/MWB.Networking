using MWB.Networking.Layer0_Transport.Encoding;

namespace MWB.Networking.Layer1_Framing.Encoding.Abstractions;

/// <summary>
/// Encodes serialized NetworkFrame bytes for outbound transmission
/// (e.g. framing, compression, encryption).
/// </summary>
public interface IFrameEncoder
{
    /// <summary>
    /// Encodes a single logical frame into one or more byte segments and
    /// emits the encoded result to the downstream sink.
    /// </summary>
    /// <param name="input">
    /// The byte segments representing exactly one complete logical frame.
    /// </param>
    /// <param name="output">
    /// The downstream sink that receives the encoded frame.
    /// </param>
    /// <param name="ct">
    /// A cancellation token used to cancel the operation.
    /// </param>
    ValueTask EncodeFrameAsync(
        ByteSegments input,
        IFrameEncoderSink output,
        CancellationToken ct);
}
