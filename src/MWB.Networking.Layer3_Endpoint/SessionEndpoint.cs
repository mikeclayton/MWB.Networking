using Microsoft.Extensions.Logging;

namespace MWB.Networking.Layer3_Endpoint;

/// <summary>
/// Hosts a live protocol session over a transport.
/// Owns lifecycle and policy, but not protocol semantics or transport wiring.
/// </summary>
public sealed partial class SessionEndpoint : IAsyncDisposable
{

    // ------------------------------------------------------------
    // Construction
    // ------------------------------------------------------------

    internal SessionEndpoint(
        ILogger logger,
        IProtocolRuntimeFactory runtimeFactory,
        SessionEndpointObservers observers
    )
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.RuntimeFactory = runtimeFactory ?? throw new ArgumentNullException(nameof(runtimeFactory));
        this.Observers = observers ?? throw new ArgumentNullException(nameof(observers));
    }

    // construction parameters

    private ILogger Logger
    {
        get;
    }

    private IProtocolRuntimeFactory RuntimeFactory
    {
        get;
    }

    private SessionEndpointObservers Observers
    {
        get;
    }
}
