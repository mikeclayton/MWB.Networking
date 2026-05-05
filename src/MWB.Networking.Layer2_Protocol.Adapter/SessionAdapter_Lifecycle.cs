using MWB.Networking.Logging;

namespace MWB.Networking.Layer2_Protocol.Adapter;

public sealed partial class SessionAdapter
{
    // ------------------------------------------------------------------
    // Lifecycle state machine
    // ------------------------------------------------------------------

    /// <summary>
    /// Encapsulates the execution lifecycle of a <see cref="ProtocolDriver"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="DriverLifecycle"/> exists to centralise and enforce the temporal
    /// invariants of driver execution: a driver may be started at most once,
    /// stopped idempotently, and must be able to shut down cooperatively via
    /// cancellation.
    /// </para>
    /// <para>
    /// This type is intentionally private and non‑generic. It does not define
    /// policy or protocol semantics; it exists solely to make the driver's
    /// start/stop mechanics explicit and structurally safe, rather than relying
    /// on convention or call ordering.
    /// </para>
    /// </remarks>
    private sealed class DriverLifecycle
    {
        private readonly CancellationTokenSource _cts = new();
        private Task? _runTask;

        public Task Start(Func<CancellationToken, Task> run)
        {
            if (_runTask is not null)
            {
                throw new InvalidOperationException("Driver already started.");
            }

            _runTask = run(_cts.Token);
            return _runTask;
        }

        public async Task StopAsync()
        {
            _cts.Cancel();

            if (_runTask is not null)
            {
                try
                {
                    await _runTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown
                }
            }
        }
    }

    // ------------------------------------------------------------------
    // Lifecycle
    // ------------------------------------------------------------------

    private readonly DriverLifecycle _lifecycle = new();

    private readonly TaskCompletionSource _whenStartedSource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Completes when the driver execution loops have been scheduled.
    /// </summary>
    public Task WhenStarted => _whenStartedSource.Task;

    /// <summary>
    /// Starts executing the driver loops. May be called once.
    /// </summary>
    public Task RunAsync(CancellationToken ct)
    {
        using var scope = this.Logger.BeginMethodLoggingScope(this);

        return _lifecycle.Start(token =>
        {
            var linkedCts =
                CancellationTokenSource.CreateLinkedTokenSource(ct, token);

            return this.RunInternalAsync(linkedCts.Token);
        });
    }

    /// <summary>
    /// Requests cooperative shutdown of the driver and waits for execution
    /// to complete. Safe to call multiple times.
    /// </summary>
    public Task StopAsync()
    {
        return _lifecycle.StopAsync();
    }

    private void SignalStarted()
    {
        _whenStartedSource.TrySetResult();
    }
}
