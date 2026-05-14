using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Driver.Abstractions;
using MWB.Networking.Layer2_Protocol.Hosting;
using MWB.Networking.Layer2_Protocol.Session;
using System.Threading.Channels;

namespace MWB.Networking.Layer2_Protocol.Adapter;

public sealed partial class SessionAdapter : IDisposable
{
    private readonly CancellationTokenSource _cts = new();

    private bool _disposed;

    internal SessionAdapter(
        ILogger logger,
        ProtocolSessionBuilder sessionBuilder,
        INetworkFrameSink transport)

    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.FrameSink = transport ?? throw new ArgumentNullException(nameof(transport));

        // call the sessionbuilder to create a session with the adapter injected as the action sinks
        this.Session = (sessionBuilder ?? throw new ArgumentNullException(nameof(sessionBuilder))).Build(
            incomingActions: this, outgoingActions: this);

        _queue = Channel.CreateUnbounded<Action>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        _ = this.RunAsync();
    }

    private ILogger Logger
    {
        get;
    }

    private ProtocolSession Session
    {
        get;
    }

    private INetworkFrameSink FrameSink
    {
        get;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, true))
        {
            // was already disposed
            return;
        }

        _disposed = true;
        _cts.Cancel();
    }
}
