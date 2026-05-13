using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Driver.Abstractions;
using MWB.Networking.Layer2_Protocol.Session.Api;
using System.Threading.Channels;

namespace MWB.Networking.Layer2_Protocol.Adapter;

public sealed partial class SessionAdapter :
    IDisposable
{
    private readonly ILogger _logger;
    private readonly ProtocolSessionHandle _session;
    private readonly INetworkFrameSink _transport;

    private readonly CancellationTokenSource _cts = new();

    internal SessionAdapter(
        ILogger logger,
        ProtocolSessionHandle session,
        INetworkFrameSink transport)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));

        _queue = Channel.CreateUnbounded<Action>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        _ = this.RunAsync();
    }

    public void Dispose()
    {
        _cts.Cancel();
    }
}
