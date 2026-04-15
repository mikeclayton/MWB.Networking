using Microsoft.Extensions.Logging;
using MWB.Networking.Logging;
using System.Net;
using System.Net.Sockets;

namespace MWB.Networking.Layer0_Transport.Tcp;

public sealed class TcpNetworkConnection : INetworkConnection, IHasLogger, IHasId, IHasDisplayName, IDisposable
{
    const int MaxFrameSize = 16 * 1024 * 1024; // 16 MB

    private volatile TcpClient? _client;
    private volatile NetworkStream? _stream;

    public TcpNetworkConnection(
        ILogger logger,
        IPEndPoint remoteEndpoint,
        CancellationToken shutdownToken = default)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.RemoteEndpoint = remoteEndpoint;
        this.ShutdownToken = shutdownToken;
    }

    public ILogger Logger
    {
        get;
    }

    public Guid Id
    {
        get;
    } = Guid.NewGuid();

    public string DisplayName
        => this.RemoteEndpoint.ToString();

    private IPEndPoint RemoteEndpoint
    {
        get;
    }

    private CancellationToken ShutdownToken
    {
        get;
    }

    private SemaphoreSlim ConnectLock
    {
        get;
    } = new(1, 1);

    private TaskCompletionSource ConnectedTcs
    {
        get;
        set;
    } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private int _started;

    private int Started
    {
        get => _started;
    }

    private int SetStarted(int value)
    {
        return Interlocked.Exchange(ref _started, value);
    }

    /// <summary>
    /// Starts the background reconnect loop.
    /// Must be called exactly once.
    /// </summary>
    [LogMethod]
    public Task StartAsync()
    {
        if (this.SetStarted(1) == 1)
        {
            throw new InvalidOperationException($"{nameof(TcpNetworkConnection)} already started.");
        }
        return Task.Run(this.ReconnectLoopAsync, this.ShutdownToken);
    }

    [LogMethod]
    public async Task WaitUntilConnectedAsync(CancellationToken ct = default)
    {
        using var linked =
            CancellationTokenSource.CreateLinkedTokenSource(ct, this.ShutdownToken);
        await this.ConnectedTcs.Task.WaitAsync(linked.Token);
    }

    /// <summary>
    /// Writes:
    ///
    /// [ total block length ][ segment ][ segment ] … [ segment ]
    /// </summary>
    /// <param name="header"></param>
    /// <param name="payload"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="IOException"></exception>
    [LogMethod]
    public async Task WriteBlockAsync(
        ReadOnlyMemory<byte>[] segments,
        CancellationToken ct)
    {
        // Wait until a connection is established (or cancellation requested)
        await this.WaitUntilConnectedAsync(ct).ConfigureAwait(false);

        try
        {
            var s = _stream ?? throw new IOException("Not connected.");
            await LengthPrefixedBlockHelpers.WriteBlockAsync(s, segments, ct);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            // Connection dropped mid-write – trigger reconnect
            this.HandleDisconnect();
            throw new IOException("Write failed.", ex);
        }
    }

    [LogMethod]
    public async Task<byte[]> ReadBlockAsync(CancellationToken ct)
    {
        // Wait until a connection is established (or cancellation requested)
        await this.WaitUntilConnectedAsync(ct).ConfigureAwait(false);

        // capture the current stream so it can't change during this method
        var s = _stream ?? throw new IOException("Not connected.");
        try
        {
            var buffer = await LengthPrefixedBlockHelpers.ReadBlockAsync(
                s, TcpNetworkConnection.MaxFrameSize, ct);
            return buffer;
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            this.HandleDisconnect();
            throw new IOException("Receive failed.", ex);
        }
    }

    [LogMethod]
    private async Task ReconnectLoopAsync()
    {
        while (!this.ShutdownToken.IsCancellationRequested)
        {
            try
            {
                await this.EnsureConnectedAsync(this.ShutdownToken);
                await Task.Delay(Timeout.Infinite, this.ShutdownToken);
            }
            catch (OperationCanceledException)
            {
                // normal termination path
                break;
            }
            catch
            {
                // something went wrong - wait and then try to reconnect
                await Task.Delay(TimeSpan.FromSeconds(1), this.ShutdownToken);
            }
        }
    }

    [LogMethod]
    private async Task EnsureConnectedAsync(CancellationToken ct)
    {
        await this.ConnectLock.WaitAsync(ct);
        try
        {
            if (_client != null)
            {
                return;
            }
            var tcp = new TcpClient
            {
                // prevent Nagle's algorithm for ultra-low latency
                // on user input events (mouse, keyboard)
                NoDelay = true
            };
            await tcp.ConnectAsync(this.RemoteEndpoint, ct);

            _client = tcp;
            _stream = tcp.GetStream();

            this.ConnectedTcs.TrySetResult();
        }
        finally
        {
            this.ConnectLock.Release();
        }
    }

    [LogMethod]
    private void HandleDisconnect()
    {
        _client?.Dispose();
        _client = null;
        _stream = null;

        lock (this)
        {
            if (!this.ConnectedTcs.Task.IsCompleted)
            {
                this.ConnectedTcs.TrySetCanceled();
            }

            this.ConnectedTcs = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    [LogMethod]
    public void Dispose()
    {
        // Tear down network resources defensively.
        // Assume background tasks may still be unwinding due to shutdownToken.
        try
        {
            _stream?.Dispose();
            _client?.Dispose();
        }
        finally
        {
            _stream = null;
            _client = null;
        }

        // Wake up any waiters so they do not deadlock during shutdown.
        lock (this)
        {
            if (!this.ConnectedTcs.Task.IsCompleted)
            {
                this.ConnectedTcs.TrySetCanceled();
            }
        }

        // Dispose synchronization primitives last.
        this.ConnectLock.Dispose();
    }
}
