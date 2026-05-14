using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;
using MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Streams;

internal sealed partial class StreamManager
{
    internal StreamManager(
        ILogger logger,
        ProtocolSession session,
        OddEvenStreamIdProvider streamIdProvider)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.Actions = new StreamActions(this);

        this.StreamIdProvider = streamIdProvider ?? throw new ArgumentNullException(nameof(streamIdProvider));

        this.Inbound = new StreamManagerInbound(logger, session, this, this.Actions, this.StreamContexts);
        this.Outbound = new StreamManagerOutbound(logger, session, this, this.Actions, this.StreamContexts, streamIdProvider);
    }

    private ILogger Logger
    {
        get;
    }

    private ProtocolSession Session
    {
        get;
    }

    private StreamActions Actions
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
