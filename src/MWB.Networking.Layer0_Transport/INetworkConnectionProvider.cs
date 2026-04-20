namespace MWB.Networking.Layer0_Transport;

/// <summary>
/// Factory for establishing a logical network connection.
/// </summary>
/// <remarks>
/// <see cref="INetworkConnectionProvider"/> is responsible for creating and
/// initializing a <see cref="LogicalConnectionHandle"/>, including any
/// transport‑specific connection setup.
/// </remarks>
public interface INetworkConnectionProvider : IDisposable
{
    Task<LogicalConnectionHandle> OpenConnectionAsync(CancellationToken ct);
}
