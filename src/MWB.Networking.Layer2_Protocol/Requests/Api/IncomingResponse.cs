namespace MWB.Networking.Layer2_Protocol.Requests.Api;

/// <summary>
/// Represents a response received by the local peer for an <see cref="OutgoingRequest"/>.
/// Provides read-only access to the payload and metadata carried by the terminal
/// Response or Error frame sent by the remote peer.
/// </summary>
public sealed class IncomingResponse
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
}
