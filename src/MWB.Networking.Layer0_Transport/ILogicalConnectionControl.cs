namespace MWB.Networking.Layer0_Transport;

/// <summary>
/// Provides privileged control over a logical network connection.
/// </summary>
/// <remarks>
/// <see cref="ILogicalConnectionControl"/> is intended for use by
/// <see cref="INetworkConnection"/> implementers to perform
/// infrastructure‑level operations that mutate the physical transport
/// backing a logical connection.
/// </remarks>
public interface ILogicalConnectionControl
{

    /// <summary>
    /// Attaches a physical network connection as the current backing
    /// connection for the logical connection.
    /// </summary>
    /// <remarks>
    /// Any previously attached backing connection is replaced and disposed.
    /// </remarks>
    void Attach(INetworkConnection connection);
}
