using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Logging;

namespace MWB.Networking.Layer0_Transport;

/// <summary>
/// Provides a concrete implementation of a logical, full‑duplex network connection.
/// </summary>
/// <remarks>
/// <see cref="LogicalConnection"/> is a stable, long‑lived connection
/// implementation of <see cref="ILogicalConnection"/> that is exposed to higher
/// layers, while allowing the underlying <see cref="INetworkConnection"/>
/// to be attached, replaced, or disposed transparently by infrastructure components.
/// </remarks>
public sealed class LogicalConnection :
    ILogicalConnection,
    ILogicalConnectionControl,
    IDisposable
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

    private INetworkConnection ActiveConnection =>
        _activeConnection
        ?? throw new InvalidOperationException(
            "Logical connection has no active backing connection.");

    private INetworkConnection? SwapActiveConnection(INetworkConnection? value) =>
        Interlocked.Exchange(ref _activeConnection, value);

    private volatile TaskCompletionSource _ready =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private TaskCompletionSource Ready => _ready;

    private TaskCompletionSource SwapReady(TaskCompletionSource value)
        => Interlocked.Exchange(ref _ready, value);

    void ILogicalConnectionControl.Attach(INetworkConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        using var scope = this.Logger.BeginMethodLoggingScope(this);

        var old = this.SwapActiveConnection(connection);
        old?.Dispose();

        this.Ready.TrySetResult();
    }

    public Task WhenReadyAsync(CancellationToken ct) =>
        this.Ready.Task.WaitAsync(ct);

    /// <summary>
    /// Reads raw bytes from the active network connection.
    /// Returns 0 on EOF.
    /// </summary>
    public async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken ct)
    {
        await this.WhenReadyAsync(ct).ConfigureAwait(false);
        return await this.ActiveConnection.ReadAsync(buffer, ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Writes raw byte segments to the active network connection.
    /// </summary>
    public async ValueTask WriteAsync(
        ByteSegments segments,
        CancellationToken ct)
    {
        await this.WhenReadyAsync(ct).ConfigureAwait(false);
        await this.ActiveConnection.WriteAsync(segments, ct)
            .ConfigureAwait(false);
    }

    public void Dispose()
    {
        var old = this.SwapActiveConnection(null);
        old?.Dispose();

        this.SwapReady(new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously))
            .TrySetCanceled();
    }
}
