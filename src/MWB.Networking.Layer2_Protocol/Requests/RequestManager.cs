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
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(session);
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.Inbound = new RequestManagerInbound(session, this, this.RequestContexts);
        this.Outbound = new RequestManagerOutbound(session, this, this.RequestContexts);
    }

    public ILogger Logger
    {
        get;
    }

    private ProtocolSession Session
    {
        get;
    }

    private RequestContexts RequestContexts
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
        return this.RequestContexts.GetRequestContextIds();
    }

    internal bool TryGetRequestContext(uint requestId, [NotNullWhen(true)] out RequestContext? result)
    {
        return this.RequestContexts.TryGetRequestContext(requestId, out result);
    }

    internal void RemoveRequest(uint requestId)
    {
        // Look up the request context first
        if (!this.RequestContexts.TryGetRequestContext(requestId, out var context))
        {
            // not a valid request
            throw new InvalidOperationException();
        }

        // auto-close streams owned by the request
        this.Session.StreamManager.TearDownRequestStreams(requestId);

        this.Inbound.RemoveIncomingRequest(context);
        this.RequestContexts.RemoveRequestContext(requestId);
    }
}
