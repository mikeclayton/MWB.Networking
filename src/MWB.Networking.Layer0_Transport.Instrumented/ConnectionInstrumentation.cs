using MWB.Networking.Layer0_Transport.Encoding;

namespace MWB.Networking.Layer0_Transport.Instrumented;

/// <summary>
/// Provides a test-only control surface for an <see cref="INetworkConnection"/>,
/// allowing deterministic control of lifecycle transitions and I/O behavior.
/// 
/// This type exists solely to support TransportStack testing and exposes
/// capabilities that are intentionally unavailable in production transports.
/// </summary>
public sealed class ConnectionInstrumentation
{
    internal ConnectionInstrumentation(InstrumentedNetworkConnection connection)
    {
        this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    private InstrumentedNetworkConnection Connection
    {
        get;
    }

    // ------------------------------------------------------------------
    // Test instrumentation
    // ------------------------------------------------------------------

    /// <summary>
    /// Injects raw bytes that will be returned by the next
    /// <see cref="ReadAsync"/> call.
    /// </summary>
    public void InjectBytes(ReadOnlyMemory<byte> frame)
    {
        this.Connection.InjectBytes(frame);
    }

    /// <summary>
    /// Returns all data written via <see cref="WriteAsync"/>.
    /// </summary>
    public IReadOnlyCollection<ByteSegments> GetWrites()
        => this.Connection.GetWrites();

    // --- Lifecycle control (test-facing API) ----------------

    public void SignalConnecting()
        => this.Connection.SignalConnecting();

    public void SignalConnected()
        => this.Connection.SignalConnected();

    /// <summary>
    /// Convenience: signals Connecting then Connected in a single call.
    /// Equivalent to calling <see cref="SimulateConnecting"/> followed by
    /// <see cref="SimulateConnected"/>.
    /// </summary>
    public void OnStarted()
    {
        this.SignalConnecting();
        this.SignalConnected();
    }

    public void SignalDisconnecting()
        => this.Connection.SignalDisconnecting();

    public void SignalDisconnected(string reason)
        => this.Connection.SignalDisconnected(reason);

    public void SignalFaulted(string reason, Exception? exception = null)
        => this.Connection.SignalFaulted(reason, exception);

    public void SetNextReadException(Exception exception)
        => this.Connection.SetNextReadFailure(exception);
}
