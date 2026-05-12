using MWB.Networking.Layer2_Protocol.Internal;
using System.Diagnostics.CodeAnalysis;

namespace MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

internal sealed class RequestEntries
{
    // ------------------------------------------------------------------
    // Cached request entries
    // ------------------------------------------------------------------

    private readonly Dictionary<uint, RequestEntry> _requestEntries = [];

    internal void AddRequestEntry(RequestEntry entry)
    {
        if (!_requestEntries.TryAdd(entry.RequestId, entry))
        {
            throw new ProtocolException(
                ProtocolErrorKind.DuplicateRequestId,
                $"A request with ID {entry.RequestId} already exists in this session");
        }
    }

    internal bool RequestEntryExists(uint requestId)
    {
        return _requestEntries.ContainsKey(requestId);
    }

    internal bool TryGetRequestEntry(uint requestId, [NotNullWhen(true)] out RequestEntry? result)
    {
        return _requestEntries.TryGetValue(requestId, out result);
    }

    internal List<uint> GetRequestEntryIds()
    {
        return _requestEntries.Keys.ToList();
    }

    internal bool RemoveRequestEntry(uint requestId)
    {
        return _requestEntries.Remove(requestId);
    }

    // ------------------------------------------------------------------
    // Utility methods
    // ------------------------------------------------------------------

    internal void EnsureRequestDoesNotExist(
        uint requestId)
    {
        if (this.RequestEntryExists(requestId))
        {
            throw ProtocolException.InvalidSequence(
                $"Duplicate RequestId {requestId}");
        }
    }

    internal RequestEntry EnsureRequestExists(
        uint requestId)
    {
        if (this.TryGetRequestEntry(requestId, out var result))
        {
            return result;
        }
        throw ProtocolException.InvalidSequence(
            $"Unknown or completed RequestId {requestId}");
    }
}
