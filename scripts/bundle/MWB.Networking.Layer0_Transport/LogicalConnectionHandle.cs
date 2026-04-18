namespace MWB.Networking.Layer0_Transport;

/// <summary>
/// Bundles the safe logical connection and its privileged control surface.
/// The control interface must only be used by transport providers.
/// </summary>
public sealed class LogicalConnectionHandle
{
    internal LogicalConnectionHandle(
        LogicalConnection connection)
    {
        this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        this.Control = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <summary>
    /// Safe, consumer-facing logical connection.
    /// </summary>
    public ILogicalConnection Connection
    {
        get;
    }

    /// <summary>
    /// Privileged control surface for transport providers.
    /// Infrastructure-only - here be dragons.
    /// </summary>
    public ILogicalConnectionControl Control
    {
        get;
    }
}
