namespace MWB.Networking.Layer2_Protocol.Driver;

public sealed partial class ProtocolDriver
{

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

        public CancellationToken Token => _cts.Token;

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
}
