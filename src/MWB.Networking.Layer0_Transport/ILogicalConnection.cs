namespace MWB.Networking.Layer0_Transport;


/// <summary>
/// Represents a logical, full-duplex network connection.
/// The underlying physical transport may be replaced transparently
/// (e.g. due to reconnect or arbitration), but this abstraction
/// presents a single stable connection to higher layers.
/// </summary>
public interface ILogicalConnection : INetworkConnection
{

    /// <summary>
    /// Completes when the logical connection is ready for I/O.
    /// The underlying physical transport may be arbitrated,
    /// connected, disconnected, or replaced transparently.
    /// </summary>
    Task WhenReadyAsync(CancellationToken ct);
}
