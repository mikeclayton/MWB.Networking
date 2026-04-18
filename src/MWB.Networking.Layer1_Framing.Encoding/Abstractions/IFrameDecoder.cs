using System.Buffers;

namespace MWB.Networking.Layer1_Framing.Encoding.Abstractions
{
    /// <summary>
    /// Decodes inbound frame bytes
    /// (e.g. decryption, decompression, deframing).
    /// </summary>
    public interface IFrameDecoder
    {
        /// <summary>
        /// Decodes an incoming byte stream and emits fully reconstructed
        /// logical frames to the downstream sink.
        /// </summary>
        /// <param name="input">
        /// A sequence of bytes received from the transport. May contain
        /// partial frames, complete frames, or multiple frames.
        /// </param>
        /// <param name="output">
        /// The sink that receives fully decoded frames.
        /// </param>
        /// <param name="ct">
        /// A cancellation token used to cancel the operation.
        /// </param>
        ValueTask DecodeFrameAsync(
            ReadOnlySequence<byte> input,
            IFrameDecoderSink output,
            CancellationToken ct = default);
    
        /// <summary>
        /// Signals that no more input will arrive and the decoder
        /// should flush any buffered state and emit any final frames.
        /// </summary>
        ValueTask CompleteAsync(
            IFrameDecoderSink output,
            CancellationToken ct = default);
    }
}