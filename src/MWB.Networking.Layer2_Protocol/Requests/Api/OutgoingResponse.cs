namespace MWB.Networking.Layer2_Protocol.Requests.Api;

/// <summary>
/// Represents a response sent by the local peer in reply to an <see cref="IncomingRequest"/>.
/// Returned as a confirmation handle after <see cref="IncomingRequest.Respond"/> or
/// <see cref="IncomingRequest.Reject"/> has been called.
/// </summary>
public sealed class OutgoingResponse
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
    public uint RequestId
    {
        get;
    }

    /// <summary>
    /// The response-type discriminator that was sent, if any.
    /// </summary>
    public uint? ResponseType
    {
        get;
    }

    /// <summary>
    /// Indicates whether this response was sent as a protocol-level error.
    /// </summary>
    public bool IsError
    {
        get;
    }
}
