using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed partial class RequestManager
{
    // ------------------------------------------------------------------
    // Request handling
    // ------------------------------------------------------------------

    private RequestEntries RequestEntries
    {
        get;
    } = new();

    internal List<uint> GetRequestEntryIds()
    {
        return this.RequestEntries.GetRequestEntryIds();
    }

    internal void RemoveRequest(uint requestId)
    {
        // Look up the request context first
        if (!this.RequestEntries.TryGetRequestEntry(requestId, out var entry))
        {
            // not a valid request
            throw new InvalidOperationException(
                $"Cannot remove request {requestId}: the request does not exist or has already completed.");
        }

        // auto-close streams owned by the request
        this.Session.StreamManager.TearDownRequestStreams(requestId);

        // Remove the request lifecycle entry (terminal)
        // The request must no longer be observable by the time the response is transmitted.
        this.RequestEntries.RemoveRequestEntry(requestId);
    }
}
