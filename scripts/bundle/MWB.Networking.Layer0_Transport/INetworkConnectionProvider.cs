namespace MWB.Networking.Layer0_Transport;

public interface INetworkConnectionProvider : IDisposable
{
    Task<LogicalConnectionHandle> OpenConnectionAsync(CancellationToken ct);
}
