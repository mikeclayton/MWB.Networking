using MWB.Networking.Layer0_Transport.Stack.Abstractions;

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
    /// When <see langword="true"/>, every connection created by the provider
    /// will be in loopback mode: bytes written via <c>WriteAsync</c> are
    /// routed directly to the read channel and returned by <c>ReadAsync</c>.
    /// When <see langword="false"/> (the default), writes are recorded in the
    /// write buffer and the read channel is fed only by explicit
    /// <see cref="ConnectionInstrumentation.InjectBytes"/> calls.
    /// Must be set before the connection is opened.
    /// </summary>
    public bool UseLoopback
    {
        get => this.Provider.UseLoopback;
        set => this.Provider.UseLoopback = value;
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