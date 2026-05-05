using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Logging;
using System.Diagnostics.CodeAnalysis;

namespace MWB.Networking.Layer2_Protocol.Requests;

internal sealed class RequestManager : IHasLogger
{
    internal RequestManager(ILogger logger, ProtocolSession session)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.Inbound = new RequestManagerInbound(session, this, this.RequestEntries);
        this.Outbound = new RequestManagerOutbound(session, this, this.RequestEntries);
    }

    public ILogger Logger
    {
        get;
    }

    private ProtocolSession Session
    {
        get;
    }

    private RequestEntries RequestEntries
    {
        get;
    } = new();

    internal RequestManagerInbound Inbound
    {
        get;
    }

    internal RequestManagerOutbound Outbound
    {
        get;
    }

    // ------------------------------------------------------------------
    // Request handling
    // ------------------------------------------------------------------

    internal static bool IsTerminalRequestFrame(ProtocolFrame frame)
    {
        return frame.Kind == ProtocolFrameKind.Response
            || frame.Kind == ProtocolFrameKind.Error;
    }

    internal IEnumerable<uint> GetRequestIds()
    {
        return this.RequestEntries.GetRequestEntryIds();
    }

    internal bool TryGetRequestEntry(uint requestId, [NotNullWhen(true)] out RequestEntry? result)
    {
        return this.RequestEntries.TryGetRequestEntry(requestId, out result);
    }

    internal void RemoveRequest(uint requestId)
    {
        // Look up the request context first
        if (!this.RequestEntries.TryGetRequestEntry(requestId, out var entry))
        {
            // not a valid request
            throw new InvalidOperationException();
        }

        // auto-close streams owned by the request
        this.Session.StreamManager.TearDownRequestStreams(requestId);

        this.Inbound.RemoveRequestEntry(entry);
    }
}
