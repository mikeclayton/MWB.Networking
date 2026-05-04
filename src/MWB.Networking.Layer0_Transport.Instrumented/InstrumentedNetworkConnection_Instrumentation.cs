using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer0_Transport.Lifecycle.Abstractions;
using MWB.Networking.Layer0_Transport.Lifecycle.Stack;

namespace MWB.Networking.Layer0_Transport.Instrumented;

/// <summary>
/// Deterministic, manually driven test implementation of <see cref="INetworkConnection"/>.
///
/// Contains no background threads or I/O loops. All behavior is
/// explicitly controlled by test code.
///
/// Supports explicit lifecycle signaling via <see cref="OnStarted"/> and
/// <see cref="Disconnect"/>.
/// </summary>
public sealed partial class InstrumentedNetworkConnection : INetworkConnection, IDisposable
{
    private Exception? _nextReadFailure;

    // ------------------------------------------------------------------
    // Test instrumentation
    // ------------------------------------------------------------------

    public ConnectionInstrumentation Instrumentation
    {
        get;
    }

    /// <summary>
    /// Injects raw bytes that will be returned by the next
    /// <see cref="ReadAsync"/> call.
    /// </summary>
    internal void InjectBytes(ReadOnlyMemory<byte> frame)
    {
        if (_disposed || _isDisconnected || _isFaulted)
            return;

        _readChannel.Writer.TryWrite(frame);
    }

    /// <summary>
    /// Returns all data written via <see cref="WriteAsync"/>.
    /// </summary>
    internal IReadOnlyCollection<ByteSegments> GetWrites()
        => _writes.ToArray();

    // --- Lifecycle control (test-facing API) ----------------

    internal void SignalConnecting()
        => _status.OnConnecting();

    internal void SignalConnected()
        => _status.OnConnected();

    internal void SignalDisconnecting()
        => _status.OnDisconnecting();

    internal void SignalDisconnected(string reason)
        => this.Disconnect(reason);

    internal void SignalFaulted(string reason, Exception? exception = null)
    {
        if (_isFaulted)
            return;

        _isFaulted = true;

        _readChannel.Writer.TryComplete();

        _status.OnFaulted(new TransportFaultedEventArgs(reason, exception));
    }

    /// <summary>
    /// Configures the next <see cref="OpenConnectionAsync"/> call to throw
    /// the supplied exception instead of returning a connection.
    /// Only the immediately following call is affected; subsequent calls
    /// behave normally.
    /// </summary>
    internal void SetNextReadFailure(Exception exception)
    {
        if (_nextReadFailure is not null)
        {
            throw new InvalidOperationException(
                "A read failure is already configured. " +
                "Only one failure can be queued at a time.");
        }
        _nextReadFailure =
            exception ?? throw new ArgumentNullException(nameof(exception));
    }
}