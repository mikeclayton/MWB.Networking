namespace MWB.Networking.Layer0_Transport.Instrumented;

public sealed partial class InstrumentedNetworkConnectionProvider
{
    private readonly List<InstrumentedNetworkConnection> _connections = new();
    private Exception? _nextOpenConnectionFailure;

    /// <summary>
    /// When <see langword="true"/>, every connection created by
    /// <see cref="OpenConnectionAsync"/> will be in loopback mode: bytes
    /// written via <see cref="INetworkConnection.WriteAsync"/> are routed
    /// directly to the read channel and returned by
    /// <see cref="INetworkConnection.ReadAsync"/>.
    /// When <see langword="false"/> (the default), writes are recorded in
    /// the write buffer and the read channel is fed only by explicit
    /// <see cref="ConnectionInstrumentation.InjectBytes"/> calls.
    /// </summary>
    internal bool UseLoopback { get; set; }

    public ProviderInstrumentation Instrumentation
    {
        get;
    }

    /// <summary>
    /// Gets all connections created by this provider, in the order they
    /// were opened. Useful for reconnect scenario testing.
    /// </summary>
    internal IReadOnlyList<InstrumentedNetworkConnection> Connections
        => _connections;

    /// <summary>
    /// Configures the next <see cref="OpenConnectionAsync"/> call to throw
    /// the supplied exception instead of returning a connection.
    /// Only the immediately following call is affected; subsequent calls
    /// behave normally.
    /// </summary>
    internal void SetNextOpenConnectionFailure(Exception exception)
    {
        if (_nextOpenConnectionFailure is not null)
        {
            throw new InvalidOperationException(
                "An open connection failure is already configured. " +
                "Only one failure can be queued at a time.");
        }
        _nextOpenConnectionFailure =
            exception ?? throw new ArgumentNullException(nameof(exception));
    }
}