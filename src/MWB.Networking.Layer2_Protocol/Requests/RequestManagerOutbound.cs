using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed partial class RequestManagerOutbound
{
    private const uint _firstRequestId = 1;
    private uint _nextRequestId = _firstRequestId;

    internal RequestManagerOutbound(
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

    /// <summary>
    /// Generate a new unique request ID.
    /// </summary>
    private uint GetNextRequestId()
    {
        if (_nextRequestId == uint.MaxValue)
        {
            throw new ProtocolException(ProtocolErrorKind.InternalError, "Request id pool exhausted.");
        }
        var requestId = _nextRequestId;
        _nextRequestId++;
        return requestId;
    }
}
