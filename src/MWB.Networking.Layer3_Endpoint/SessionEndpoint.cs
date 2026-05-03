using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Runtime;

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

    public SessionEndpoint(
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

    public ILogger Logger
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
