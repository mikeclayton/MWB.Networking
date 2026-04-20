namespace MWB.Networking.Layer0_Transport.Tcp.Arbitration;

/// <summary>
/// Defines a deterministic policy for deciding whether an incoming
/// TCP connection should replace an existing active connection.
/// </summary>
internal interface ITcpConnectionArbitrator
{
    /// <summary>
    /// Determines whether an incoming connection should replace
    /// the currently active connection.
    /// </summary>
    bool ShouldReplace(
        ConnectionDirection currentConnectionDirection,
        ConnectionDirection candidateConnectionDirection);
}