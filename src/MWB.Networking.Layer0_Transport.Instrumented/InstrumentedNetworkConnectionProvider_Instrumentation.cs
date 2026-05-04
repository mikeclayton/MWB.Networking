namespace MWB.Networking.Layer0_Transport.Instrumented;

public sealed partial class InstrumentedNetworkConnectionProvider
{
    private readonly List<InstrumentedNetworkConnection> _connections = new();
    private Exception? _nextOpenConnectionFailure;

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