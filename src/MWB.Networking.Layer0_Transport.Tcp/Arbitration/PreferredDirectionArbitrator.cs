namespace MWB.Networking.Layer0_Transport.Tcp.Arbitration;

/// <summary>
/// A deterministic connection arbitrator that prefers connections
/// originating from a specific direction.
/// </summary>
internal sealed class PreferredDirectionArbitrator : ITcpConnectionArbitrator
{
    private readonly ConnectionDirection _preferredDirection;

    public PreferredDirectionArbitrator(ConnectionDirection preferredDirection)
    {
        _preferredDirection = preferredDirection;
    }

    public bool ShouldReplace(
        ConnectionDirection incoming,
        ConnectionDirection active)
    {
        return incoming == _preferredDirection
            && active != _preferredDirection;
    }
}
