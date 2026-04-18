using MWB.Networking.Layer0_Transport.Encoding;

namespace MWB.Networking.Layer0_Transport;

/// <summary>
/// Represents a low-level, transport-oriented network connection capable of
/// sending and receiving raw byte buffers.
///
/// Implementations are responsible for establishing and maintaining network
/// connectivity, including reconnecting after transient failures. This interface
/// does not provide message framing, buffering, retries, or protocol semantics;
/// higher layers are responsible for those concerns.
/// </summary>
public interface INetworkConnection : IDisposable
{
    /// <summary>
    /// Writes raw byte segments to the underlying transport.
    /// </summary>
    /// <param name="segments">
    /// A collection of byte segments representing a contiguous logical write.
    /// Segment boundaries are preserved where supported by the transport.
    /// </param>
    /// <param name="ct">
    /// A cancellation token used to cancel the write operation.
    /// </param>
    /// <returns>
    /// A task that completes when all segments have been written.
    /// </returns>
    /// <remarks>
    /// This method treats the provided segments as an opaque sequence of bytes.
    /// It does not interpret frame boundaries, encoding, compression, or
    /// encryption semantics. Such concerns are handled by higher layers.
    /// </remarks>
    ValueTask WriteAsync(ByteSegments segments, CancellationToken ct);

    /// <summary>
    /// Reads raw bytes from the underlying transport into the provided buffer.
    /// </summary>
    /// <param name="buffer">
    /// The destination buffer to fill with bytes read from the transport.
    /// </param>
    /// <param name="ct">
    /// A cancellation token used to cancel the read operation.
    /// </param>
    /// <returns>
    /// The number of bytes read into <paramref name="buffer"/>.
    /// Returns zero to indicate end-of-stream.
    /// </returns>
    /// <remarks>
    /// This method operates on raw bytes only and has no knowledge of framing,
    /// encoding, or protocol semantics. Higher layers are responsible for
    /// interpreting the received data.
    /// </remarks>
    ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct);
}
