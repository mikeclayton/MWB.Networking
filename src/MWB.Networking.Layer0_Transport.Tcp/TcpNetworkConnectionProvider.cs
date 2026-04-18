using Microsoft.Extensions.Logging;
using MWB.Networking.Logging;
using System.Net.Sockets;

namespace MWB.Networking.Layer0_Transport.Tcp;

public sealed class TcpNetworkConnectionProvider
    : INetworkConnectionProvider
{
    public TcpNetworkConnectionProvider(ILogger logger, TcpNetworkConnectionConfig config)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.Config = config ?? throw new ArgumentNullException(nameof(config));
        this.LogicalConnection = LogicalConnectionFactory.Create(logger);
    }

    public ILogger Logger
    {
        get;
    }

    private TcpNetworkConnectionConfig Config
    {
        get;
    }

    private LogicalConnectionHandle LogicalConnection
    {
        get;
    }

    private Lock LockObject
    {
        get;
    } = new();

    private TcpNetworkConnection? ActiveConnection
    {
        get;
        set;
    }

    private TcpListener? Listener
    {
        get;
        set;
    }

    private CancellationTokenSource? _cts;
    private Task? _listenerTask;
    private Task? _outboundTask;

    public async Task<LogicalConnectionHandle> OpenConnectionAsync(
        CancellationToken ct)
    {
        using var logScope = this.Logger.BeginMethodLoggingScope(this);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        if (this.Config.LocalEndpoint is not null)
        {
            _listenerTask = this.StartListenerAsync(_cts.Token);
        }

        if (this.Config.RemoteEndpoint is not null)
        {
            _outboundTask = this.StartOutboundConnectLoopAsync(_cts.Token);
        }

        var handle = this.LogicalConnection;
        await handle.Connection.WhenReadyAsync(ct);

        return handle;
    }

    private async Task StartListenerAsync(CancellationToken ct)
    {
        using var logScope = this.Logger.BeginMethodLoggingScope(this);

        this.Listener = new TcpListener(this.Config.LocalEndpoint!);
        this.Listener.Start();

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var client = await this.Listener.AcceptTcpClientAsync(ct);
                client.NoDelay = this.Config.NoDelay;

                this.AttachCandidate(
                    new TcpNetworkConnection(client, this.Config.MaxFrameSize),
                    ConnectionDirection.Inbound);
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
    }

    private async Task StartOutboundConnectLoopAsync(CancellationToken ct)
    {
        using var logScope = this.Logger.BeginMethodLoggingScope(this);

        // Defensive: no remote endpoint means nothing to do
        if (this.Config.RemoteEndpoint is null)
        {
            this.Logger.LogDebug("no remote endpoint configured- returning from method");
            return;
        }

        while (!ct.IsCancellationRequested)
        {
            //this.Logger.LogDebug("entering (busy) connect loop");

            // If we already have an active logical connection,
            // do NOT keep dialing. Layer 0's job is to establish
            // a connection, not compete with an existing one.
            if (this.ActiveConnection is not null)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1),ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                continue;
            }

            try
            {
                var client = new TcpClient
                {
                    NoDelay = this.Config.NoDelay
                };

                // attempt outbound connect
                this.Logger.LogDebug("attempting connect to {Endpoint}", this.Config.RemoteEndpoint);
                await client.ConnectAsync(
                    this.Config.RemoteEndpoint!.Address,
                    this.Config.RemoteEndpoint.Port,
                    ct);
                this.Logger.LogDebug("outbound connect established");

                // Wrap the socket in a physical transport
                var connection = new TcpNetworkConnection(
                    client,
                    this.Config.MaxFrameSize);

                // Hand candidate to arbitrator
                this.AttachCandidate(
                    connection,
                    ConnectionDirection.Outbound);

                // IMPORTANT:
                // Do NOT return.
                // Even if this connection "wins", we must remain alive
                // in case it later fails and a reconnect is needed.
            }
            catch (OperationCanceledException)
            {
                this.Logger.LogDebug("connection closed normally");
                break;
            }
            catch (SocketException ex)
            {
                this.Logger.LogDebug(ex, "connect failed, will retry");
            }
            catch (Exception ex)
            {

                this.Logger.LogWarning(ex, "unexpected error");
            }

            // Backoff before retrying
            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(1),
                    ct);
            }
            catch (OperationCanceledException)
            {
                this.Logger.LogDebug("connection closed normally");
                break;
            }
        }
    }

    private void AttachCandidate(
        TcpNetworkConnection candidate,
        ConnectionDirection direction)
    {
        using var logScope = this.Logger.BeginMethodLoggingScope(this);

        // lock to ensure critical section below is protected,
        // but don't await while holding the lock
        using var lockScope = this.LockObject.EnterScope();

        var control = this.LogicalConnection.Control;
        if (this.ActiveConnection is null)
        {
            this.ActiveConnection = candidate;
            control.Attach(candidate);
            this.Logger.LogDebug(
                "AttachCandidate accepted connection (direction={Direction})",
                direction);
            return;
        }

        if (TcpNetworkConnectionProvider.WinsOverExisting(direction))
        {
            this.ActiveConnection.Dispose();
            this.ActiveConnection = candidate;
            control.Attach(candidate);
        }
        else
        {
            candidate.Dispose();
        }
    }

    private static bool WinsOverExisting(
        ConnectionDirection incoming)
    {
        // simple deterministic policy:
        // inbound beats outbound
        return incoming == ConnectionDirection.Inbound;
    }

    public void Dispose()
    {
        // stop all background activity first
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        // stop accepting inbound connections
        this.Listener?.Stop();
        this.Listener = null;

        // lock to ensure critical section below is protected,
        // but don't await while holding the lock
        using var lockScope = this.LockObject.EnterScope();

        this.ActiveConnection?.Dispose();
        this.ActiveConnection = null;

        // CRITICAL: terminate the logical connection itself
        // This unblocks WhenReadyAsync, read loops, decoders, and protocol consumers
        this.LogicalConnection.Connection.Dispose();
    }
}
