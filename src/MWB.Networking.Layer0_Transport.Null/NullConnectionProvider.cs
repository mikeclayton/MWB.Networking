using Microsoft.Extensions.Logging;

namespace MWB.Networking.Layer0_Transport.NullTransport;

public sealed class NullConnectionProvider
    : INetworkConnectionProvider
{
    public NullConnectionProvider(ILogger logger)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private ILogger Logger
    {
        get;
    }

    private static INetworkConnection Connection
    {
        get;
    } = new NullConnection();

    public Task<LogicalConnectionHandle> OpenConnectionAsync(CancellationToken ct)
    {
        // opening a null connection is instantaneous and side-effect free.
        // Cancellation is irrelevant here.
        var handle = LogicalConnectionFactory.Create(this.Logger);
        return Task.FromResult(handle);
    }

    public void Dispose()
    {
        // nothing to do here
        return;
    }
}
