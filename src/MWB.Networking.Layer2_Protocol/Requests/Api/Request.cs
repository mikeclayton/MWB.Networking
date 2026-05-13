using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests.Internal;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Requests.Api;

/// <summary>
/// Represents a request delivered to or emitted by the application.
///
/// This is the application-facing projection of a protocol request and includes
/// both the request metadata and associated payload. Instances of this type are
/// materialized at publication or transmission time and do not participate in
/// protocol lifecycle or invariant enforcement.
/// </summary>
public sealed class Request
{
    internal Request(
        RequestContext context,
        RequestActions actions,
        ReadOnlyMemory<byte> payload)
    {
        this.Context = context ?? throw new ArgumentNullException(nameof(context));
        this.Actions = actions ?? throw new ArgumentNullException(nameof(actions));
        this.Payload = payload;
    }

    private RequestContext Context
    {
        get;
    }

    public bool CanRespond
        => this.Context.Direction == ProtocolDirection.Incoming;

    private RequestActions Actions
    {
        get;
    }

    public uint RequestId
        => this.Context.RequestId;

    public uint? RequestType
        => this.Context.RequestType;

    public ReadOnlyMemory<byte> Payload
    {
        get;
    }

    /// <summary>
    /// Sends a normal (non-error) Response for this Request and closes it.
    /// </summary>
    public Response Respond(uint? responseType = null, ReadOnlyMemory<byte> payload = default)
    {
        return this.Actions.Respond(this.Context, responseType, payload);
    }

    /// <summary>
    /// Opens the single Request-scoped SessionStream for this Request.
    /// </summary>
    public SessionStream OpenRequestStream(uint? streamType)
    {
        return this.Actions.OpenRequestStream(this.Context, streamType);
    }
}
