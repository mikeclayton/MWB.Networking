using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;
using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Requests.Api;

/// <summary>
/// Represents a request received from the remote peer.
/// </summary>
public sealed class IncomingRequest
{
    internal IncomingRequest(
        RequestManager requestManager,
        RequestContext context)
    {
        this.RequestManager = requestManager ?? throw new ArgumentNullException(nameof(requestManager));
        this.Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    internal RequestManager RequestManager
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
    public OutgoingResponse Respond(uint? responseType = null, ReadOnlyMemory<byte> payload = default)
    {
        return this.RequestManager.Actions.Respond(this.Context, responseType, payload);
    }

    /// <summary>
    /// Sends an error Response for this Request and closes the Request.
    /// </summary>
    public OutgoingResponse Reject(ReadOnlyMemory<byte> payload = default)
    {
        return this.RequestManager.Actions.Reject(this.Context, payload);
    }

    /// <summary>
    /// Opens the single Request-scoped Stream for this Request.
    /// </summary>

    public OutgoingStream OpenRequestStream(uint? streamType)
    {
        // validate request is open
        this.Context.OpenStream();

        // delegate to ProtocolSession
        return this.RequestManager.Session.StreamManager.Outbound.OpenRequestStream(streamType, this.Context);
    }
}
