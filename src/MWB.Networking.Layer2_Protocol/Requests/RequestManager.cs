using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;
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

    internal RequestActions Actions
    {
        get;
    }
}
