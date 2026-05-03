using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Requests.Api;

internal sealed class IncomingRequests
{
    // ------------------------------------------------------------------
    // IncomingRequest cache - only use methods, don't access the field directly
    // ------------------------------------------------------------------

    private readonly Dictionary<RequestContext, IncomingRequest>
        _cachedIncomingRequests = [];

    internal void AddIncomingRequest(IncomingRequest request)
    {
        _cachedIncomingRequests.Add(request.Context, request);
    }

    internal IncomingRequest? GetIncomingRequest(RequestContext? context)
    {
        // Null means this is a session-scoped stream or operation
        if (context is null)
        {
            return null;
        }

        // There must be exactly one IncomingRequest per RequestContext.
        // If this lookup fails, it indicates a protocol or lifecycle bug.
        if (!_cachedIncomingRequests.TryGetValue(context, out var incomingRequest))
        {
            throw new InvalidOperationException(
                "RequestContext has no associated IncomingRequest. " +
                "This indicates an internal request lifecycle inconsistency.");
        }

        return incomingRequest;
    }

    internal void RemoveIncomingRequest(RequestContext context)
    {
        _cachedIncomingRequests.Remove(context);
    }
}
