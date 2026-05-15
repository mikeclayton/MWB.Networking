using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

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
        RequestContext context,
        uint? responseType,
        ReadOnlyMemory<byte> payload,
        bool isError)
    {
        this.Context = context ?? throw new ArgumentNullException(nameof(context));
        this.ResponseType = responseType;
        this.Payload = payload;
        this.IsError = isError;
    }

    private RequestContext Context
    {
        get;
    }

    /// <summary>
    /// The protocol RequestId this response corresponds to.
    /// </summary>
    public uint RequestId
        => this.Context.RequestId;

    /// <summary>
    /// The optional response-type discriminator. The value is specified by
    /// the application process, and has no inherent meaning to the protocol.
    /// </summary>
    public uint? ResponseType
    {
        get;
    }

    private ProtocolDirection Direction
        => this.Context.Direction;

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
}
