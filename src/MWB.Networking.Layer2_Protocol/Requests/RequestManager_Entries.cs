using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed partial class RequestManager
{
    // ------------------------------------------------------------------
    // Request handling
    // ------------------------------------------------------------------

    private RequestContexts RequestContexts
    {
        get;
    } = new();

    internal IReadOnlyCollection<uint> GetRequestIds()
    {
        return this.RequestContexts.GetRequestIds();
    }

    internal void RemoveRequest(RequestContext context)
    {      
        // auto-close streams owned by the request
        this.Session.StreamManager.TearDownRequestStreams(context.RequestId);

        // Remove the request lifecycle entry before transmitting the response
        // to prevent re-entrant lookup during transmission
        this.RequestContexts.Remove(context.RequestId);
    }
}
