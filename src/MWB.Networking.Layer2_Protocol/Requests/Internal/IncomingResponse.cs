using MWB.Networking.Layer2_Protocol.Requests.Api;

namespace MWB.Networking.Layer2_Protocol.Requests.Internal;

/// <summary>
/// Represents a terminal protocol response or error received from the remote peer
/// for a request initiated by the local peer.
///
/// This type is part of the internal protocol core and models response
/// identity and protocol semantics only. It does not carry payload data and
/// must not be exposed directly to application code.
/// </summary>
/// <remarks>
/// Payload data associated with the response is delivered separately at
/// publication time or via application-facing response projections.
/// </remarks>
internal sealed class IncomingResponse
{
    internal IncomingResponse(
        uint requestId,
        uint? responseType,
        bool isError)
    {
        this.IsError = isError;
        this.RequestId = requestId;
        this.ResponseType = responseType;
    }

    /// <summary>
    /// Indicates whether this response represents a protocol-level error.
    /// </summary>
    internal bool IsError
    {
        get;
    }

    /// <summary>
    /// The protocol RequestId this response corresponds to.
    /// </summary>
    internal uint RequestId
    {
        get;
    }

    /// <summary>
    /// The optional response-type discriminator sent by the remote peer.
    /// </summary>
    internal uint? ResponseType
    {
        get;
    }

    /// <summary>
    /// Projects this internal response into an application-facing <see cref="Response"/>,
    /// attaching the provided payload for publication or transmission. The
    /// publishable form must not be used internally for protocol-level processing
    /// or validation.
    /// </summary>
    public Response AsPublishable(ReadOnlyMemory<byte> payload)
    {
        return new Response(
            this.RequestId, this.ResponseType, this.IsError, payload);
    }
}
