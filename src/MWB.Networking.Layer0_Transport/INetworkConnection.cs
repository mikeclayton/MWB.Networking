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
public interface INetworkConnection
{
    /// <summary>
    /// Asynchronously waits until the underlying connection is established and
    /// ready for I/O.
    ///
    /// This method completes successfully once the connection is usable. It may
    /// block across transient disconnects and reconnect attempts.
    ///
    /// The method throws or cancels if the provided cancellation token is triggered.
    /// </summary>
    /// <param name="ct">
    /// A cancellation token used to abort waiting for connectivity, typically during shutdown.
    /// </param>
    Task WaitUntilConnectedAsync(CancellationToken ct);

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
