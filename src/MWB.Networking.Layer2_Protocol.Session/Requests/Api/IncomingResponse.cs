namespace MWB.Networking.Layer2_Protocol.Session.Requests.Api;

/// <summary>
/// Represents a response received by the local peer for an <see cref="OutgoingRequest"/>.
/// Provides read-only access to the payload and metadata carried by the terminal
/// Response or Error frame sent by the remote peer.
/// </summary>
public sealed class IncomingResponse
{
    internal IncomingResponse(
        bool isError,
        uint requestId,
        uint? responseType,
        ReadOnlyMemory<byte> payload)
    {
        this.IsError = isError;
        this.RequestId = requestId;
        this.ResponseType = responseType;
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

    /// <summary>
    /// The payload carried by the terminal response frame.
    /// </summary>
    public ReadOnlyMemory<byte> Payload
    {
        get;
    }
}
