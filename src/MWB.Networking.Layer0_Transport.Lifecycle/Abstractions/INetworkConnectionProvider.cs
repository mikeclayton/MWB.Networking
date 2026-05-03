using MWB.Networking.Layer0_Transport.Lifecycle.Stack;

namespace MWB.Networking.Layer0_Transport.Lifecycle.Abstractions;

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
        ObservableConnectionStatus status, CancellationToken ct);
}
