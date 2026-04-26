using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Runtime;
using MWB.Networking.Layer2_Protocol.Session.Api;

namespace MWB.Networking.Layer3_Hosting.Runtime;

/// <summary>
/// Hosts a live protocol session over a transport.
/// Owns lifecycle, execution, and reconnection policy but not construction.
/// </summary>
public sealed class SessionHost : IAsyncDisposable
{
    public SessionHost(
        ILogger logger,
        IProtocolInstanceFactory instanceFactory)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.InstanceFactory = instanceFactory ?? throw new ArgumentNullException(nameof(instanceFactory));
    }

    // construction parameters

    public ILogger Logger
    {
        get;
    }

    private IProtocolInstanceFactory InstanceFactory
    {
        get;
    }


    // current instances

    private ProtocolInstance? ActiveInstance
    {
        get;
        set;
    }

    private ProtocolInstance GetActiveInstance()
    {
        var instance = this.ActiveInstance;
        if (instance is null)
        {
            throw new InvalidOperationException(
                "Session has not been started.");
        }
        return instance;
    }

    private readonly object _gate = new();

    // ------------------------------------------------------------
    // ProtocolSession facade
    // ------------------------------------------------------------

    public IProtocolSessionCommands Commands
        => this.GetActiveInstance().ProtocolSession.Commands;

    public Task WhenReady
        => this.GetActiveInstance().ProtocolSession.WhenReady;

    public IProtocolSessionObserver Observers
        => this.GetActiveInstance().ProtocolSession.Observer;

    // ------------------------------------------------------------
    // SessionHost members
    // ------------------------------------------------------------

    /// <summary>
    /// Starts the hosted protocol runtime.
    /// </summary>
    public async Task StartAsync(CancellationToken ct)
    {
        lock (_gate)
        {
            if (this.ActiveInstance is not null)
            {
                throw new InvalidOperationException("Runtime already started.");
            }
        }

        // Create a new runnable protocol instance
        var runtime =
            await this.InstanceFactory
                .CreateAsync(ct)
                .ConfigureAwait(false);

        lock (_gate)
        {
            this.ActiveInstance = runtime;
        }

        // Start execution
        await runtime.ProtocolDriver
            .RunAsync(ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Stops the hosted protocol runtime.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        ProtocolInstance? instance;

        lock (_gate)
        {
            instance = this.ActiveInstance;
            this.ActiveInstance = null;
        }

        if (instance is not null)
        {
            await instance.ProtocolDriver
                .StopAsync()
                .ConfigureAwait(false);
        }
    }
}
