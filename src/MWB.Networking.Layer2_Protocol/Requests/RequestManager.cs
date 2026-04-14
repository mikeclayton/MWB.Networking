using MWB.Networking.Layer2_Protocol.Session;
using System.Diagnostics.CodeAnalysis;

namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed partial class RequestManager
{
    internal RequestManager(ProtocolSession session)
    {
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
    }

    private ProtocolSession Session
    {
        get;
    }

    // ------------------------------------------------------------------
    // Request handling
    // ------------------------------------------------------------------

    private Dictionary<uint, RequestContext> RequestContexts
    {
        get;
    } = [];

    private uint NextRequestId
    {
        get;
        set;
    } = 1;

    internal static bool IsTerminalRequestFrame(ProtocolFrame frame)
    {
        return frame.Kind == ProtocolFrameKind.Response
            || frame.Kind == ProtocolFrameKind.Error;
    }

    internal IEnumerable<uint> GetRequestIds()
    {
        return this.RequestContexts.Keys;
    }

    internal bool TryGetRequestContext(uint key, [NotNullWhen(true)] out RequestContext? result)
    {
        return this.RequestContexts.TryGetValue(key, out result);
    }

    private void RemoveRequest(uint requestId)
    {
        // auto-close streams owned by the request
        this.Session.StreamManager.TearDownRequestStreams(requestId);

        this.RequestContexts.Remove(requestId);
    }
}
