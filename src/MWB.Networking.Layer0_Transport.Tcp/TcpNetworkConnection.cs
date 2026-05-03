using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer0_Transport.Lifecycle.Abstractions;
using MWB.Networking.Layer0_Transport.Lifecycle.Stack;
using System.Net.Sockets;

namespace MWB.Networking.Layer0_Transport.Tcp;

/// <summary>
/// Represents a single physical TCP connection attempt.
/// </summary>
/// <remarks>
/// This type reports transport lifecycle events via
/// <see cref="ObservableConnectionStatus"/> but does not
/// participate in reconnection or policy decisions.
/// </remarks>
public sealed class TcpNetworkConnection
    : INetworkConnection, IDisposable
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly int _maxFrameSize;

    private ObservableConnectionStatus? _status;
    private bool _started;
    private bool _disposed;

    public TcpNetworkConnection(
        TcpClient client,
        int maxFrameSize)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _stream = client.GetStream();
        _maxFrameSize = maxFrameSize;
    }

    // ------------------------------------------------------------------
    // Lifecycle binding (called by provider)
    // ------------------------------------------------------------------

    internal void BindStatus(ObservableConnectionStatus status)
    {
        if (_status is not null)
            throw new InvalidOperationException("Status already bound.");

        _status = status ?? throw new ArgumentNullException(nameof(status));
    }

    /// <summary>
    /// Signals that the TCP connection has been established and
    /// the transport is ready for I/O.
    /// </summary>
    internal void OnStarted()
    {
        if (_started)
            return;

        _started = true;

        _status!.OnConnecting();
        _status.OnConnected();
    }

    // ------------------------------------------------------------------
    // INetworkConnection
    // ------------------------------------------------------------------

    public async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken ct)
    {
        ThrowIfDisposed();

        try
        {
            int bytesRead =
                await _stream.ReadAsync(buffer, ct)
                             .ConfigureAwait(false);

            if (bytesRead == 0)
            {
                // EOF: orderly remote shutdown
                _status?.OnDisconnected(
                    new TransportDisconnectedEventArgs(
                        "Remote endpoint closed the TCP connection."));
            }

            return bytesRead;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _status?.OnFaulted(
                new TransportFaultedEventArgs(
                    "TCP read failed.",
                    ex));
            throw;
        }
    }

    public async ValueTask WriteAsync(
        ByteSegments segments,
        CancellationToken ct)
    {
        ThrowIfDisposed();

        try
        {
            foreach (var segment in segments.Segments)
            {
                if (!segment.IsEmpty)
                {
                    await _stream.WriteAsync(
                        segment,
                        ct).ConfigureAwait(false);
                }
            }

            await _stream.FlushAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _status?.OnFaulted(
                new TransportFaultedEventArgs(
                    "TCP write failed.",
                    ex));
            throw;
        }
    }

    // ------------------------------------------------------------------
    // Disposal
    // ------------------------------------------------------------------

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, true))
        {
            // was already disposed
            return;
        }

        try
        {
            _stream.Close();
            _client.Close();
        }
        finally
        {

            // Disposal is an observable, orderly termination:
            // the TCP connection is no longer usable.
            _status?.OnDisconnected(
                new TransportDisconnectedEventArgs(
                    "TCP transport disposed."));
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(TcpNetworkConnection));
    }
}