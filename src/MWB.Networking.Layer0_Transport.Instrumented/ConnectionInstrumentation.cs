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
    /// <see langword="true"/> when the connection was created in loopback mode:
    /// bytes written via <see cref="WriteAsync"/> are routed directly to the
    /// read channel and returned by <see cref="ReadAsync"/>.
    /// <see langword="false"/> when in normal mode: writes are recorded in the
    /// write buffer (inspectable via <see cref="GetWrites"/>) and the read
    /// channel is fed only by explicit <see cref="InjectBytes"/> calls.
    /// </summary>
    public bool IsLoopback => this.Connection.IsLoopback;

    /// <summary>
    /// Injects raw bytes that will be returned by the next
    /// <see cref="ReadAsync"/> call.
    /// </summary>
    public void InjectBytes(ReadOnlyMemory<byte> frame)
    {
        this.Connection.InjectBytes(frame);
    }

    public int ReadChannelCount
        => this.Connection.ReadChannelCount;

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
