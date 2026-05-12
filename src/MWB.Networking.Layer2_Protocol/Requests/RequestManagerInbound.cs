using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed partial class RequestManagerInbound
{
    internal RequestManagerInbound(
        ILogger logger,
        RequestManager requestManager,
        RequestEntries requestEntries)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.RequestManager = requestManager ?? throw new ArgumentNullException(nameof(requestManager));
        this.RequestEntries = requestEntries ?? throw new ArgumentNullException(nameof(requestEntries));
    }

    private ILogger Logger
    {
        get;
    }

    private RequestManager RequestManager
    {
        get;
    }

    private RequestEntries RequestEntries
    {
        get;
    }
}
