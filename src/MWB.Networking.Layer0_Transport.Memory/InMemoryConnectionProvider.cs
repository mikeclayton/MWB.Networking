using MWB.Networking.Layer0_Transport.Memory.Buffer;
using MWB.Networking.Layer0_Transport.Stack.Core.Connection;
using MWB.Networking.Layer0_Transport.Stack.Core.Lifecycle;

namespace MWB.Networking.Layer0_Transport.Memory;

/// <summary>
/// Network connection provider that exposes one side of an
/// in-memory full-duplex transport.
/// </summary>
/// <remarks>
/// This provider is lifecycle-agnostic. It binds an
/// <see cref="ObservableConnectionStatus"/> to the selected
/// <see cref="InMemoryConnection"/> and signals that the
/// connection has started.
/// </remarks>
public sealed class InMemoryNetworkConnectionProvider
    : INetworkConnectionProvider
{
    private readonly SegmentedDuplexBuffer _buffer;
    private readonly SegmentedDuplexBufferSide _side;

    private bool _opened;

    public InMemoryNetworkConnectionProvider(
        SegmentedDuplexBuffer buffer,
        SegmentedDuplexBufferSide side)
    {
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        _side = side;
    }

    public Task<INetworkConnection> OpenConnectionAsync(
        IConnectionStatus status,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (_opened)
            throw new InvalidOperationException(
                "This provider can only open one connection instance.");

        _opened = true;

        var connection = _buffer.GetConnection(_side);

        // Bind lifecycle state for this connection attempt
        connection.BindStatus(status);

        // Signal that wiring is complete and the connection is usable
        connection.OnStarted();

        return Task.FromResult<INetworkConnection>(connection);
    }

    public void Dispose()
    {
        // Intentionally no-op.
        // The TransportStack owns connection lifetime.
    }
}
