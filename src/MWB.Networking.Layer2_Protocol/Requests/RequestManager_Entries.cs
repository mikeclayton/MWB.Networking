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
}
