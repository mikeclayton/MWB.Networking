using MWB.Networking.Layer2_Protocol.Internal;

namespace MWB.Networking.Layer2_Protocol.Requests.Api;

/// <summary>
/// Represents a response delivered to or emitted by the application.
/// </summary>
/// <remarks>
/// Encapsulates the response metadata and payload associated with a request.
/// Responses are immutable data objects and do not participate in request
/// lifecycle or protocol invariant enforcement.
/// </remarks>
public abstract class PublicResponse
{
    internal PublicResponse(
        uint requestId,
        uint? responseType,
        ReadOnlyMemory<byte> payload,
        bool isError,
        ProtocolDirection direction)
    {
        this.RequestId = requestId;
        this.ResponseType = responseType;
        this.Payload = payload;
        this.IsError = isError;
        this.Direction = direction;
    }

    /// <summary>
    /// The protocol RequestId this response corresponds to.
    /// </summary>
    public uint RequestId
    {
        get;
    }

    /// <summary>
    /// The optional response-type discriminator sent by the remote peer.
    /// </summary>
    public uint? ResponseType
    {
        get;
    }

    /// <summary>
    /// The payload associated with this response.
    /// </summary>
    public ReadOnlyMemory<byte> Payload
    {
        get;
    }

    /// <summary>
    /// Indicates whether this response represents a protocol-level error.
    /// </summary>
    public bool IsError
    {
        get;
    }

    internal ProtocolDirection Direction
    {
        get;
    }
}
