using MWB.Networking.Layer2_Protocol.Requests.Api;

namespace MWB.Networking.Layer2_Protocol.Requests.Internal;

/// <summary>
/// Represents a terminal protocol response or error sent by the local peer
/// in reply to an incoming request.
///
/// This type is part of the internal protocol core and models protocol
/// response identity and error semantics. It does not carry payload data
/// and must not be exposed directly to application code.
/// </summary>
/// <remarks>
/// Instances of this type are created during outbound response consumption
/// and are transmitted to the remote peer after request lifecycle closure.
/// </remarks>
internal sealed class OutgoingResponse
{
    internal OutgoingResponse(
        uint requestId,
        uint? responseType,
        bool isError)
    {
        this.RequestId = requestId;
        this.ResponseType = responseType;
        this.IsError = isError;
    }

    /// <summary>
    /// The protocol RequestId this response was sent for.
    /// </summary>
    internal uint RequestId
    {
        get;
    }

    /// <summary>
    /// The response-type discriminator that was sent, if any.
    /// </summary>
    internal uint? ResponseType
    {
        get;
    }

    /// <summary>
    /// Indicates whether this response was sent as a protocol-level error.
    /// </summary>
    internal bool IsError
    {
        get;
    }

    /// <summary>
    /// Projects this internal request into an application-facing <see cref="Response"/>,
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
