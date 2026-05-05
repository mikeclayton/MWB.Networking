using MWB.Networking.Layer2_Protocol.Frames;
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
}
