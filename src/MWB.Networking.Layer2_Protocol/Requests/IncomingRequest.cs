using MWB.Networking.Layer2_Protocol.Streams;

namespace MWB.Networking.Layer2_Protocol.Requests;

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
        this.Session.CloseRequestWithResponse(this.Context, payload);
    }

    /// <summary>
    /// Sends an error Response for this Request and closes the Request.
    /// </summary>
    public void Fail(ReadOnlyMemory<byte> payload)
    {
        this.Session.CloseRequestWithError(this.Context, payload);
    }

    /// <summary>
    /// Opens the single Request-scoped Stream for this Request.
    /// </summary>

    public IncomingStream OpenRequestStream()
    {
        // validate request is open
        this.Context.OpenStream();

        // delegate to ProtocolSession
        return this.Session.OpenRequestStream(this.Context);
    }
}