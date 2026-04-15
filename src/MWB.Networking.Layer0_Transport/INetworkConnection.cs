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
    /// Writes exactly one data block to the connection, prefixing it with a length header.
    /// </summary>
    /// <remarks>
    /// The caller provides the complete block payload.
    /// The transport is responsible for transmitting the block atomically
    /// with respect to block boundaries.
    /// Calls must be serialized by the caller.
    /// </remarks>
    Task WriteBlockAsync(
        ReadOnlyMemory<byte>[] segments,
        CancellationToken ct);

    /// <summary>
    /// Receives exactly one length-prefixed data block from the connection.
    /// </summary>
    /// <remarks>
    /// This method blocks until a complete block is available.
    /// It never returns partial data, and preserves block boundaries.
    /// Calls must be serialized by the caller.
    /// </remarks>
    Task<byte[]> ReadBlockAsync(CancellationToken ct);
}
