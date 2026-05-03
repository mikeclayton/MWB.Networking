using MWB.Networking.Layer2_Protocol.Frames;
using System.Diagnostics.CodeAnalysis;

namespace MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

internal partial class RequestContexts
{
    // ------------------------------------------------------------------
    // Cached request contexts
    // ------------------------------------------------------------------

    private readonly Dictionary<uint, RequestContext> _requestContexts = [];

    internal void AddRequestContext(RequestContext context)
    {
        if (!_requestContexts.TryAdd(context.RequestId, context))
        {
            throw new ProtocolException(
                ProtocolErrorKind.DuplicateRequestId,
                $"A request with ID {context.RequestId} is already in flight.");
        }
    }

    internal bool RequestContextExists(uint requestId)
    {
        return _requestContexts.ContainsKey(requestId);
    }

    internal bool TryGetRequestContext(uint requestId, [NotNullWhen(true)] out RequestContext? result)
    {
        return _requestContexts.TryGetValue(requestId, out result);
    }

    internal List<uint> GetRequestContextIds()
    {
        return _requestContexts.Keys.ToList();
    }

    internal bool RemoveRequestContext(uint requestId)
    {
        return _requestContexts.Remove(requestId);
    }
}
