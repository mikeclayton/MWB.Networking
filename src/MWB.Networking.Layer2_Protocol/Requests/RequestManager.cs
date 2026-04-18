using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Logging;
using System.Diagnostics.CodeAnalysis;

namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed partial class RequestManager : IHasLogger
{
    internal RequestManager(ILogger logger, ProtocolSession session)
    {
        this.Logger = logger ?? throw new ArgumentOutOfRangeException(nameof(logger));
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public ILogger Logger
    {
        get;
    }

    private ProtocolSession Session
    {
        get;
    }

    // ------------------------------------------------------------------
    // Cached request contexts
    // ------------------------------------------------------------------

    private readonly Dictionary<uint, RequestContext> _requestContexts = [];

    private void AddRequestContext(RequestContext context)
    {
        _requestContexts.Add(context.RequestId, context);
    }

    private bool RequestContextExists(uint requestId)
    {
        return _requestContexts.ContainsKey(requestId);
    }

    internal bool TryGetRequestContext(uint requestId, [NotNullWhen(true)] out RequestContext? result)
    {
        return _requestContexts.TryGetValue(requestId, out result);
    }

    private List<uint> GetRequestContextIds()
    {
        return _requestContexts.Keys.ToList();
    }

    private bool RemoveRequestContext(uint requestId)
    {
        return _requestContexts.Remove(requestId);
    }

    // ------------------------------------------------------------------
    // Request handling
    // ------------------------------------------------------------------

    internal static bool IsTerminalRequestFrame(ProtocolFrame frame)
    {
        return frame.Kind == ProtocolFrameKind.Response
            || frame.Kind == ProtocolFrameKind.Error;
    }

    internal IEnumerable<uint> GetRequestIds()
    {
        return this.GetRequestContextIds();
    }

    private void RemoveRequest(uint requestId)
    {
        // Look up the request context first
        if (!this.TryGetRequestContext(requestId, out var context))
        {
            // not a valid request
            throw new InvalidOperationException();
        }

        // auto-close streams owned by the request
        this.Session.StreamManager.TearDownRequestStreams(requestId);

        this.RemoveCachedIncomingRequest(context);
        this.RemoveRequestContext(requestId);
    }
}
