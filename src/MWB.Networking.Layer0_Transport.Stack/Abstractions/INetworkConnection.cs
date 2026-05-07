using MWB.Networking.Layer0_Transport.Encoding;

namespace MWB.Networking.Layer0_Transport.Stack.Abstractions;

/// <summary>
/// Represents a low‑level, transport‑oriented network connection that
/// reads and writes raw bytes.
/// </summary>
/// <remarks>
/// <see cref="INetworkConnection"/> operates purely on sequences of bytes
/// - higher layers are responsible for capabilities for framing, buffering,
/// retry, or protocol semantics, etc. 
/// </remarks>
public interface INetworkConnection : IDisposable
{
    /// <summary>
    /// Writes raw byte segments to the underlying transport.
    /// </summary>
    ValueTask WriteAsync(ByteSegments segments, CancellationToken ct);

    /// <summary>
    /// Reads raw bytes from the underlying transport.
    /// Returns zero to indicate end‑of‑stream.
    /// </summary>
    ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct);
}
