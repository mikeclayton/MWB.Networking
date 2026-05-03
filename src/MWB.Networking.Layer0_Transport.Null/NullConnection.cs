using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer0_Transport.Lifecycle.Abstractions;
using MWB.Networking.Layer0_Transport.Lifecycle.Stack;

namespace MWB.Networking.Layer0_Transport.NullTransport;

public sealed class NullConnection : INetworkConnection
{
    private readonly ObservableConnectionStatus _status;
    private bool _started;
    private bool _disposed;

    public NullConnection(ObservableConnectionStatus status)
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
