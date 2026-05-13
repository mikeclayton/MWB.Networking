using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;

namespace MWB.Networking.Layer2_Protocol.Streams;

internal sealed partial class StreamManager
{
    internal StreamManager(ILogger logger, ProtocolSession session, OddEvenStreamIdProvider streamIdProvider)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(streamIdProvider);
        this.Logger = logger;
        this.StreamIdProvider = streamIdProvider;
        this.Inbound = new StreamManagerInbound(logger, session, this, this.StreamContexts);
        this.Outbound = new StreamManagerOutbound(logger, session, this, this.StreamContexts, streamIdProvider);
    }

    private ILogger Logger
    {
        get;
    }

    private OddEvenStreamIdProvider StreamIdProvider
    {
        get;
    }

    internal StreamManagerInbound Inbound
    {
        get;
    }

    internal StreamManagerOutbound Outbound
    {
        get;
    }
}
