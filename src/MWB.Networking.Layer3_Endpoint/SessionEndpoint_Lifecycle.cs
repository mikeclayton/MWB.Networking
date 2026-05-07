using MWB.Networking.Layer1_Framing.Pipeline;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Session.Frames;

namespace MWB.Networking.Layer3_Endpoint;

public sealed partial class SessionEndpoint : IAsyncDisposable
{
    // ------------------------------------------------------------
    // Accessors
    // ------------------------------------------------------------

    private ProtocolSessionHandle? _activeSession;
    private ProtocolDriver? _activeDriver;
    private NetworkPipeline? _activePipeline;

    /// <summary>
    /// Handle exposing protocol session APIs.
    /// Valid once StartAsync has been called successfully.
    /// </summary>
    private ProtocolSessionHandle GetActiveSession()
        => _activeSession
            ?? throw new InvalidOperationException(
                "Session has not been started.");

    private ProtocolDriver GetActiveDriver()
        => _activeDriver
            ?? throw new InvalidOperationException(
                "Driver has not been started.");

    private NetworkPipeline GetActivePipeline()
        => _activePipeline
            ?? throw new InvalidOperationException(
                "Pipeline has not been started.");

    // ------------------------------------------------------------
    // Lifecycle
    // ------------------------------------------------------------

    private readonly object _gate = new();
    private Task? _runTask;
    private bool _started;

    /// <summary>
    /// Builds the session endpoint and starts protocol execution.
    /// May be called at most once.
    /// </summary>
    public async Task StartAsync(CancellationToken ct)
    {
        lock (_gate)
        {
            if (_started)
            {
                throw new InvalidOperationException(
                    "SessionHost has already been started.");
            }
            _started = true;
        }

        // --------------------------------------------------------
        // Build runtime (no execution yet)
        // --------------------------------------------------------

        var (session, driver, pipeline) =
            await this.RuntimeFactory
                .CreateAsync(ct)
                .ConfigureAwait(false);

        this.Observers.RegisterObservers(session);

        _activeSession = session;
        _activeDriver = driver;
        _activePipeline = pipeline;

        // --------------------------------------------------------
        // Start execution (fire-and-forget)
        // --------------------------------------------------------

        _runTask = Task.Run(
            () => _activeDriver.RunAsync(ct),
            CancellationToken.None);
    }

    /// <summary>
    /// Stops protocol execution and releases resources.
    /// Safe to call multiple times.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        ProtocolSessionHandle? session;
        ProtocolDriver? driver;
        NetworkPipeline? pipeline;

        lock (_gate)
        {
            session = _activeSession;
            driver = _activeDriver;
            pipeline = _activePipeline;

            _activeSession = null;
            _activeDriver = null;
            _activePipeline = null;

            _started = false;
        }

        if (session is not null)
        {
            this.Observers.UnregisterObservers(session);
        }

        if (driver is not null)
        {
            await driver
                .StopAsync()
                .ConfigureAwait(false);
        }

        if (_runTask is not null)
        {
            try
            {
                await _runTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // expected on shutdown
            }
            catch (ProtocolException)
            {
                // expected if protocol violation occurred
                // swallow or log as appropriate
            }
        }

        pipeline?.Dispose();
    }
}
