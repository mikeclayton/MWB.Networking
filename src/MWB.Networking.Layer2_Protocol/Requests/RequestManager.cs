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

        this.Outbound = new RequestManagerOutbound(this, this.RequestEntries);
        this.Inbound = new RequestManagerInbound(this, this.RequestEntries);
        this.Actions = new RequestManagerActions(this, this.Session);
    }

    private ILogger Logger
    {
        get;
    }

    private ProtocolSession Session
    {
        get;
    }

    private RequestManagerOutbound Outbound
    {
        get;
    }

    private RequestManagerInbound Inbound
    {
        get;
    }

    internal RequestManagerActions Actions
    {
        get;
    }
}
