using MWB.Networking.Layer0_Transport.Lifecycle.Abstractions;

namespace MWB.Networking.Layer0_Transport.Instrumented;

/// <summary>
/// Provides a test-only control surface for an <see cref="INetworkConnectionProvider"/>,
/// allowing deterministic control of lifecycle transitions and I/O behavior.
/// 
/// This type exists solely to support TransportStack testing and exposes
/// capabilities that are intentionally unavailable in production transports.
/// </summary>
public sealed class ProviderInstrumentation
{
    internal ProviderInstrumentation(InstrumentedNetworkConnectionProvider provider)
    {
        this.Provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    private InstrumentedNetworkConnectionProvider Provider
    {
        get;
    }

    /// <summary>
    /// Gets the most recently created manual connection.
    /// Tests use this to drive lifecycle and I/O deterministically.
    /// </summary>
    public InstrumentedNetworkConnection? Connection
        => this.Provider.Connection;

    public IReadOnlyList<InstrumentedNetworkConnection> Connections
        => this.Provider.Connections;

    public void SetNextOpenConnectionFailure(Exception exception)
        => this.Provider.SetNextOpenConnectionFailure(exception);
}