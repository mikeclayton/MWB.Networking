using MWB.Networking.Layer2_Protocol.Session.Requests.Lifecycle;
using MWB.Networking.Layer2_Protocol.Session.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Session.Requests.Api;

public sealed class IncomingRequest
{
    internal IncomingRequest(
        ProtocolSession session,
        RequestContext context)
    {
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    internal ProtocolSession Session
    {
        get;
    }

    internal RequestContext Context
    {
        get;
    }

    public uint RequestId
        => this.Context.RequestId;

    public uint? RequestType
        => this.Context.RequestType;

    /// <summary>
    /// Sends the Response for this Request and closes the Request.
    /// </summary>
    public OutgoingResponse Respond(ReadOnlyMemory<byte> payload = default)
    {
        return this.Session.RequestManager.Outbound.CloseRequestWithResponse(this.Context, payload);
    }

    /// <summary>
    /// Sends an error Response for this Request and closes the Request.
    /// </summary>
    public OutgoingResponse Error(ReadOnlyMemory<byte> payload = default)
    {
        return this.Session.RequestManager.Outbound.CloseRequestWithError(this.Context, payload);
    }

    /// <summary>
    /// Opens the single Request-scoped Stream for this Request.
    /// </summary>

    public OutgoingStream OpenRequestStream(uint? streamType)
    {
        // validate request is open
        this.Context.OpenStream();

        // delegate to ProtocolSession
        return this.Session.StreamManager.Outbound.OpenRequestStream(streamType, this.Context);
    }
}
