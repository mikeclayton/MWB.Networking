namespace MWB.Networking.Layer2_Protocol.Session.Requests.Api;

/// <summary>
/// Represents a response sent by the local peer in reply to an <see cref="IncomingRequest"/>.
/// Returned as a confirmation handle after <see cref="IncomingRequest.Respond"/> or
/// <see cref="IncomingRequest.Error"/> has been called.
/// </summary>
public sealed class OutgoingResponse
{
    internal OutgoingResponse(
        ProtocolSession session,
        uint requestId,
        bool isError)
    {
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.RequestId = requestId;
        this.IsError = isError;
    }

    internal ProtocolSession Session
    {
        get;
    }

    /// <summary>
    /// The protocol RequestId this response was sent for.
    /// </summary>
    public uint RequestId
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
