using MWB.Networking.Layer2_Protocol.Session.Api;

namespace MWB.Networking.Layer3_Endpoint;

public sealed partial class SessionEndpoint : IAsyncDisposable
{
    // ------------------------------------------------------------------
    // Active runtime state
    // ------------------------------------------------------------------

    private ProtocolSessionHandle? _activeSession;
    private ProtocolRuntime? _activeRuntime;

    /// <summary>
    /// Returns the active session handle, throwing if the endpoint has not
    /// been started yet.
    /// </summary>
    private ProtocolSessionHandle GetActiveSession()
        => _activeSession
            ?? throw new InvalidOperationException(
                "Session has not been started.");

    // ------------------------------------------------------------------
    // Lifecycle
    // ------------------------------------------------------------------

    private readonly object _gate = new();
    private bool _started;

    /// <summary>
    /// Builds the runtime, registers observers, and starts I/O execution.
    /// May be called at most once.
    /// </summary>
    public async Task StartAsync(CancellationToken ct)
    {
        lock (_gate)
        {
            if (_started)
            {
                throw new InvalidOperationException(
                    "SessionEndpoint has already been started.");
            }
            _started = true;
        }

        // ----------------------------------------------------------------
        // Delegate construction and wiring to the factory.
        // The returned ProtocolRuntime is fully wired but not yet running.
        // ----------------------------------------------------------------

        var runtime = await this.RuntimeFactory
            .CreateAsync(ct)
            .ConfigureAwait(false);

        this.Observers.RegisterObservers(runtime.Session);

        _activeSession = runtime.Session;
        _activeRuntime = runtime;

        // ----------------------------------------------------------------
        // Start the transport driver's read-and-decode loop.
        // TransportDriver.Start() schedules the loop on the thread pool
        // and returns immediately; it is a fire-and-forget handoff.
        // ----------------------------------------------------------------

        runtime.Driver.Start();
    }

    /// <summary>
    /// Unregisters observers and disposes the runtime.
    /// Safe to call if <see cref="StartAsync"/> was never called.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        ProtocolSessionHandle? session;
        ProtocolRuntime? runtime;

        lock (_gate)
        {
            session = _activeSession;
            runtime = _activeRuntime;

            _activeSession = null;
            _activeRuntime = null;

            _started = false;
        }

        if (session is not null)
        {
            this.Observers.UnregisterObservers(session);
        }

        // ProtocolRuntime.Dispose() tears down in the correct order:
        //   Adapter (unsubscribe events) → Driver (cancel I/O).
        runtime?.Dispose();

        return ValueTask.CompletedTask;
    }
}
