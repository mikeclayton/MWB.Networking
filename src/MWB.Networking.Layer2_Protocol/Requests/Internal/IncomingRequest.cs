using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;
using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Requests.Internal;

/// <summary>
/// Represents a protocol request received from the remote peer.
///
/// This type is part of the internal protocol core and owns request
/// lifecycle coordination and invariant enforcement. It does not carry
/// payload data and must not be exposed directly to application code.
/// </summary>
/// <remarks>
/// Instances of this type are created during inbound request consumption
/// and are used to coordinate admission, lifecycle state, and publication
/// of an application-facing <see cref="Requests.Api.Request"/>.
/// </remarks>
internal sealed class IncomingRequest
{
    internal IncomingRequest(
        RequestContext context,
        RequestActions actions)
    {
        this.Context = context ?? throw new ArgumentNullException(nameof(context));
        this.Actions = actions ?? throw new ArgumentNullException(nameof(actions));
    }

    private RequestContext Context
    {
        get;
    }

    private RequestActions Actions
    {
        get;
    }

    internal uint RequestId
        => this.Context.RequestId;

    internal uint? RequestType
        => this.Context.RequestType;

    /// <summary>
    /// Sends a normal (non-error) Response for this Request and closes it.
    /// </summary>
    internal Response Respond(uint? responseType = null, ReadOnlyMemory<byte> payload = default)
    {
        return this.Actions.Respond(this.Context, responseType, payload);
    }

    /// <summary>
    /// Opens the single Request-scoped Stream for this Request.
    /// </summary>
    internal OutgoingStream OpenRequestStream(uint? streamType)
    {
        return this.Actions.OpenRequestStream(this.Context, streamType);
    }

    /// <summary>
    /// Projects this internal request into an application-facing <see cref="Request"/>,
    /// attaching the provided payload for publication or transmission. The
    /// publishable form must not be used internally for protocol-level processing
    /// or validation.
    /// </summary>
    public Request AsPublishable(ReadOnlyMemory<byte> payload)
    {
        return new Request(
            this.Context, this.Actions, payload);
    }
}
