using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;
using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Requests.Internal;

/// <summary>
/// Represents a protocol request initiated by the local peer.
///
/// This type is part of the internal protocol core and coordinates outbound
/// request lifecycle, sequencing, and invariant enforcement. It does not
/// represent the application-facing request abstraction.
/// </summary>
/// <remarks>
/// An application-facing <see cref="Request"/> is projected
/// from this request at publication time. This type must not be exposed
/// directly to application code.
/// </remarks>
internal sealed class OutgoingRequest
{
    internal OutgoingRequest(
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

    /// <summary>
    /// The protocol RequestId for this request.
    /// </summary>
    internal uint RequestId
        => this.Context.RequestId;

    /// <summary>
    /// Task that completes when the terminal response or error
    /// frame is received for this request.
    /// </summary>
    internal Task<IncomingResponse> Response
        => this.Context.ResponseTask;

    /// <summary>
    /// Opens the single request-scoped outgoing stream for this request.
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
