using MWB.Networking.Layer0_Transport.Stack.Core.Connection;
using MWB.Networking.Layer0_Transport.Stack.Core.Lifecycle;
using MWB.Networking.Layer0_Transport.Stack.Core.Primitives;

namespace MWB.Networking.Layer0_Transport.NullTransport;

public sealed class NullConnection : INetworkConnection
{
    private readonly IConnectionStatus _status;
    private bool _started;
    private volatile bool _disposed;

    public NullConnection(IConnectionStatus status)
    {
        _status = status ?? throw new ArgumentNullException(nameof(status));
    }

    /// <summary>
    /// Called once wiring is complete.
    /// Null connections are immediately usable.
    /// </summary>
    internal void OnStarted()
    {
        if (_started)
            return;

        _started = true;

        _status.OnConnecting();
        _status.OnConnected();
    }


    public ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken ct)
    {
        // End-of-stream immediately
        return ValueTask.FromResult(0);
    }

    public ValueTask WriteAsync(
        ByteSegments segments,
        CancellationToken ct)
    {
        // Discard silently
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, true))
        {
            // was already disposed
            return;
        }

        _status.OnDisconnected(
              new TransportDisconnectedEventArgs(
                  "Null transport disposed."));
    }
}
