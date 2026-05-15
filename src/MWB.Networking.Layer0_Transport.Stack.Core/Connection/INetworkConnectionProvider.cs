using MWB.Networking.Layer0_Transport.Stack.Core.Lifecycle;

namespace MWB.Networking.Layer0_Transport.Stack.Core.Connection;

/// <summary>
/// Factory for establishing a network connection.
/// </summary>
/// <remarks>
/// <see cref="INetworkConnectionProvider"/> is responsible for creating and
/// initializing an <see cref="INetworkConnection"/>, including any
/// transport‑specific connection setup.
/// </remarks>
public interface INetworkConnectionProvider : IDisposable
{
    Task<INetworkConnection> OpenConnectionAsync(
        IConnectionStatus status, CancellationToken ct);
}
