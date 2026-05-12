using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;
using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Requests.Api;

/// <summary>
/// Represents a request received from the remote peer.
/// </summary>
public sealed class IncomingRequest
{
    internal IncomingRequest(
        RequestContext context,
        IncomingRequestActions actions)
    {
        this.Context = context ?? throw new ArgumentNullException(nameof(context));
        this.Actions = actions ?? throw new ArgumentNullException(nameof(actions));
    }

    private RequestContext Context
    {
        get;
    }

    private IncomingRequestActions Actions
    {
        get;
    }

    public uint RequestId
        => this.Context.RequestId;

    public uint? RequestType
        => this.Context.RequestType;

    /// <summary>
    /// Sends a normal (non-error) Response for this Request and closes it.
    /// </summary>
    public OutgoingResponse Respond(uint? responseType = null, ReadOnlyMemory<byte> payload = default)
    {
        return this.Actions.Respond(this.Context, responseType, payload);
    }

    /// <summary>
    /// Opens the single Request-scoped Stream for this Request.
    /// </summary>

    public OutgoingStream OpenRequestStream(uint? streamType)
    {
        return this.Actions.OpenRequestStream(this.Context, streamType);
    }
}
