namespace MWB.Networking.Layer0_Transport;

/// <summary>
/// Bundles a logical network connection with its privileged control surface.
/// </summary>
/// <remarks>
/// <see cref="LogicalConnectionHandle"/> exists to enforce a clear boundary
/// between the consumer‑facing <see cref="ILogicalConnection"/> and the
/// privileged <see cref="ILogicalConnectionControl"/> used by transport
/// infrastructure.
/// 
/// Higher protocol layers and application code should interact exclusively
/// with the logical connection, while only network transport implementers
/// are permitted to access the control surface.
/// </remarks>
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
