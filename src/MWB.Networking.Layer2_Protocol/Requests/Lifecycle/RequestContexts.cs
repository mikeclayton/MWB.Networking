using MWB.Networking.Layer2_Protocol.Internal;
using System.Diagnostics.CodeAnalysis;

namespace MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

internal sealed class RequestContexts
{
    // ------------------------------------------------------------------
    // Cached request contexts
    // ------------------------------------------------------------------

    private readonly Dictionary<uint, RequestContext> _requestContexts = [];

    internal void Add(RequestContext context)
    {
        if (!_requestContexts.TryAdd(context.RequestId, context))
        {
            throw new ProtocolException(
                ProtocolErrorKind.DuplicateRequestId,
                $"A request with ID {context.RequestId} already exists in this session");
        }
    }

    internal bool Exists(uint requestId)
    {
        return _requestContexts.ContainsKey(requestId);
    }

    internal bool TryGet(uint requestId, [NotNullWhen(true)] out RequestContext? result)
    {
        return _requestContexts.TryGetValue(requestId, out result);
    }

    internal IReadOnlyCollection<uint> GetRequestIds()
    {
        return _requestContexts.Keys;
    }

    internal bool Remove(uint requestId)
    {
        return _requestContexts.Remove(requestId);
    }

    // ------------------------------------------------------------------
    // Utility methods
    // ------------------------------------------------------------------

    internal void ThrowIfExists(uint requestId)
    {
        if (_requestContexts.ContainsKey(requestId))
        {
            throw ProtocolException.InvalidSequence(
                $"Duplicate RequestId {requestId}");
        }
    }

    internal RequestContext GetOrThrow(uint requestId)
    {
        if (this.TryGet(requestId, out var result))
        {
            return result;
        }
        throw ProtocolException.InvalidSequence(
            $"Unknown or completed RequestId {requestId}");
    }
}
