using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport.Tcp;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Streams.Api;
using MWB.Networking.Layer3_Runtime;

namespace KeyboardSharingConsole.Networking;

public sealed class PeerConnection : IDisposable
{
    public PeerConnection(
        ILogger logger,
        TcpNetworkConnection transport,
        bool oddParity)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.Transport = transport ?? throw new ArgumentNullException(nameof(transport));
        this.Adapter = new NetworkAdapter(
            this.Transport,
            new NetworkFrameWriter(),
            new NetworkFrameReader());
        this.Session = oddParity switch
        {
            true => ProtocolSessions.CreateOddSession(),
            false => ProtocolSessions.CreateEvenSession()
        };
        this.Driver = new ProtocolDriver(this.Logger, this.Adapter, this.Session);
        this.WireProtocolEvents();
    }

    private ILogger Logger
    {
        get;
    }

    private TcpNetworkConnection Transport
    {
        get;
    }

    private NetworkAdapter Adapter
    {
        get;
    }

    private ProtocolSessionHandle Session
    {
        get;
    }

    private ProtocolDriver Driver
    {
        get;
    }

    private CancellationTokenSource LifetimeCts
    {
        get;
    } = new();

    private Task? DriverRunLoop
    {
        get;
        set;
    }

    private bool Disposed
    {
        get;
        set;
    }

    // ---- Events exposed upward ----

    public event Action<IncomingRequest>? RequestReceived;
    public event Action<IncomingStream, StreamMetadata>? StreamOpened;
    public event Action<IncomingStream>? StreamClosed;
    public event Action? Disconnected;

    // ---- Startup / shutdown ----

    public Task StartAsync(CancellationToken ct)
    {
        this.EnsureNotDisposed();

        // Link external cancellation into lifetime
        ct.Register(() => this.LifetimeCts.Cancel());

        // Start driving the protocol
        this.DriverRunLoop = this.Driver.RunAsync(this.LifetimeCts.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (this.Disposed)
        {
            return;
        }

        this.LifetimeCts.Cancel();

        if (this.DriverRunLoop is not null)
        {
            try
            {
                await this.DriverRunLoop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // expected during shutdown
            }
            catch (Exception ex)
            {
                this.Logger.LogDebug(ex, "PeerConnection driver stopped with error");
            }
        }

        this.Dispose();
    }

    // ---- Requests ----

    public OutgoingRequest SendRequest(ReadOnlyMemory<byte> payload)
    {
        this.EnsureNotDisposed();
        return this.Session.Commands.SendRequest(payload);
    }

    // ---- Streams ----

    public OutgoingStream OpenStream(ReadOnlyMemory<byte> metadata = default)
    {
        this.EnsureNotDisposed();
        return this.Session.Commands.OpenSessionStream(metadata);
    }

    // ---- Internal wiring ----

    private void WireProtocolEvents()
    {
        var observer = this.Session.Observer;
        observer.RequestReceived += (req, _) =>
        {
            this.RequestReceived?.Invoke(req);
        };

        observer.StreamOpened += (stream, metadata) =>
        {
            this.StreamOpened?.Invoke(stream, metadata);
        };

        observer.StreamClosed += (stream) =>
        {
            this.Disconnected?.Invoke();
            this.Dispose();
        };
    }

    // ---- Disposal ----

    public void Dispose()
    {
        if (this.Disposed)
        {
            return;
        }

        this.Disposed = true;

        this.LifetimeCts.Cancel();

        try
        {
            this.Transport.Dispose();
        }
        catch
        {
            // defensive
        }

        this.LifetimeCts.Dispose();
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(this.Disposed, this);
    }
}
