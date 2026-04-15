using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Requests.Api;

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

    /// <summary>
    /// Sends the Response for this Request and closes the Requet.
    /// </summary>
    public void Respond(ReadOnlyMemory<byte> payload)
    {
        this.Session.RequestManager.CloseRequestWithResponse(this.Context, payload);
    }

    /// <summary>
    /// Sends an error Response for this Request and closes the Request.
    /// </summary>
    public void Error(ReadOnlyMemory<byte> payload)
    {
        this.Session.RequestManager.CloseRequestWithError(this.Context, payload);
    }

    /// <summary>
    /// Opens the single Request-scoped Stream for this Request.
    /// </summary>

    public OutgoingStream OpenRequestStream()
    {
        // validate request is open
        this.Context.OpenStream();

        // delegate to ProtocolSession
        return this.Session.StreamManager.OpenRequestStream(this.Context);
    }
}
