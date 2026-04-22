using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;
using System.Diagnostics.CodeAnalysis;

namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed partial class RequestManager
{
    // ------------------------------------------------------------------
    // Cached request contexts
    // ------------------------------------------------------------------

    private readonly Dictionary<uint, RequestContext> _requestContexts = [];

    private void AddRequestContext(RequestContext context)
    {

        if (!_requestContexts.TryAdd(context.RequestId, context))
        {
            throw new ProtocolException(
                ProtocolErrorKind.DuplicateRequestId,
                $"A request with ID {context.RequestId} is already in flight.");
        }
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

}
