using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport.Abstractions;
using MWB.Networking.Layer0_Transport.Stack;

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


    public Task<INetworkConnection> OpenConnectionAsync(
           ObservableConnectionStatus status,
           CancellationToken ct)
    {
        // Instantaneous, side-effect free
        var connection = new NullConnection(status);

        // Signal that wiring is complete
        connection.OnStarted();

        return Task.FromResult<INetworkConnection>(connection);
    }

    public void Dispose()
    {
        // nothing to do here
        return;
    }
}
