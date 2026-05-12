using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Session;

namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed partial class RequestManager
{
    internal RequestManager(
        ILogger logger,
        ProtocolSession session)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.Session = session ?? throw new ArgumentNullException(nameof(session));

        this.Outbound = new RequestManagerOutbound(logger, this, this.RequestEntries);
        this.Inbound = new RequestManagerInbound(logger, this, this.RequestEntries);
        this.Actions = new RequestActions(this);
    }

    private ILogger Logger
    {
        get;
    }

    internal ProtocolSession Session
    {
        get;
    }

    internal RequestManagerOutbound Outbound
    {
        get;
    }

    internal RequestManagerInbound Inbound
    {
        get;
    }

    internal RequestActions Actions
    {
        get;
    }
}
