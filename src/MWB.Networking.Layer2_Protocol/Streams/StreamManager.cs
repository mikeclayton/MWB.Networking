using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;
using MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Streams;

internal sealed class StreamManager
{
    internal StreamManager(ILogger logger, ProtocolSession session, OddEvenStreamIdProvider streamIdProvider)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(streamIdProvider);
        this.Logger = logger;
        this.StreamIdProvider = streamIdProvider;
        this.Inbound = new StreamManagerInbound(session, this, this.StreamEntries);
        this.Outbound = new StreamManagerOutbound(session, this, this.StreamEntries, streamIdProvider);
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
