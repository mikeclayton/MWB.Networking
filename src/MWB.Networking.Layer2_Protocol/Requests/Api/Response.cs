namespace MWB.Networking.Layer2_Protocol.Requests.Api;

/// <summary>
/// Represents a response delivered to or emitted by the application.
///
/// This is the application-facing projection of a protocol response and includes
/// both the event metadata and associated payload. Instances of this type are
/// materialized at publication or transmission time and do not participate in
/// protocol lifecycle or invariant enforcement.
/// </summary>
public sealed class Response
{
    internal Response(
        uint requestId,
        uint? responseType,
        bool isError,
        ReadOnlyMemory<byte> payload)
    {
        this.RequestId = requestId;
        this.ResponseType = responseType;
        this.IsError = isError;
        this.Payload = payload;
    }

    /// <summary>
    /// Indicates whether this response represents a protocol-level error.
    /// </summary>
    public bool IsError
    {
        get;
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

    public ReadOnlyMemory<byte> Payload
    {
        get;
    }
}
