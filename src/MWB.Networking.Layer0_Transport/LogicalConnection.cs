using Microsoft.Extensions.Logging;
using MWB.Networking.Logging;

namespace MWB.Networking.Layer0_Transport;

public sealed class LogicalConnection : ILogicalConnection, ILogicalConnectionControl
{
    public LogicalConnection(ILogger logger)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private ILogger Logger
    {
        get;
    }

    private volatile INetworkConnection? _activeConnection;

    private INetworkConnection ActiveConnection
    {
        get => _activeConnection ?? throw new InvalidOperationException(
            "Logical connection has no active backing connection.");
    }

    private INetworkConnection? SetActiveConnection(INetworkConnection? value)
        => Interlocked.Exchange(ref _activeConnection, value);

    private volatile TaskCompletionSource _ready =
           new(TaskCreationOptions.RunContinuationsAsynchronously);

    private TaskCompletionSource Ready
        => _ready;

    private TaskCompletionSource SetReady(TaskCompletionSource value)
        => Interlocked.Exchange(ref _ready, value);

    void ILogicalConnectionControl.Attach(INetworkConnection connection)
    {
        using var logScope = this.Logger.BeginMethodScope(this);
        this.Logger.LogDebug("Entering method");

        this.AttachInternal(connection);

        this.Logger.LogDebug("Leaving method");
    }

    private void AttachInternal(INetworkConnection connection)
    {
        using var logScope = this.Logger.BeginMethodScope(this);
        this.Logger.LogDebug("Entering method");

        // Swap active physical connection
        var old = this.SetActiveConnection(connection);
        old?.Dispose();

        // Signal readiness
        this.Ready.TrySetResult();

        this.Logger.LogDebug("Leaving method");
    }

    public Task WhenReadyAsync(CancellationToken ct)
        => this.Ready.Task.WaitAsync(ct);

    public async Task<byte[]> ReadBlockAsync(CancellationToken ct)
    {
        await this.WhenReadyAsync(ct);
        var conn = this.ActiveConnection;
        return await conn.ReadBlockAsync(ct);
    }

    public async Task WriteBlockAsync(ReadOnlyMemory<byte>[] segments, CancellationToken ct)
    {
        await this.WhenReadyAsync(ct);
        var conn = this.ActiveConnection;
        await conn.WriteBlockAsync(segments, ct);
    }

    public void Dispose()
    {
        var old = this.SetActiveConnection(null);
        old?.Dispose();

        this.SetReady(new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously))
            .TrySetCanceled();
    }
}
