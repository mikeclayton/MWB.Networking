namespace MWB.Networking.Layer0_Transport;

/// <summary>
/// Represents a logical, full‑duplex network connection that serves as a
/// stable abstraction over a swappable <see cref="INetworkConnection"/>.
/// </summary>
/// <remarks>
/// The underlying <see cref="INetworkConnection"/> may be replaced
/// transparently (for example, due to reconnection or arbitration), while
/// the <see cref="ILogicalConnection"/> instance itself remains long‑lived
/// and stable for consumers in higher layers.
/// </remarks>
public interface ILogicalConnection : INetworkConnection
{
    /// <summary>
    /// Completes when the logical connection is ready for I/O.
    /// The underlying physical transport may be arbitrated,
    /// connected, disconnected, or replaced transparently.
    /// </summary>
    Task WhenConnectedAsync(CancellationToken ct);
}
