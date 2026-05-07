using MWB.Networking.Layer2_Protocol.Session.Frames;

namespace MWB.Networking.Layer2_Protocol.Session.Requests.Api;

/// <summary>
/// Represents a response received by the local peer for an <see cref="OutgoingRequest"/>.
/// Provides read-only access to the frame kind, payload, and metadata carried by
/// the terminal Response or Error frame sent by the remote peer.
/// </summary>
public sealed class IncomingResponse
{
    internal IncomingResponse(
        ProtocolSession session,
        ProtocolFrameKind kind,
        uint requestId,
        uint? responseType,
        ReadOnlyMemory<byte> payload)
    {
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.Kind = kind;
        this.RequestId = requestId;
        this.ResponseType = responseType;
        this.Payload = payload;
    }

    internal ProtocolSession Session
    {
        get;
    }

    /// <summary>
    /// The frame kind of this response (<see cref="ProtocolFrameKind.Response"/>
    /// or <see cref="ProtocolFrameKind.Error"/>).
    /// </summary>
    public ProtocolFrameKind Kind
    {
        get;
    }

    /// <summary>
    /// Indicates whether this response represents a protocol-level error.
    /// </summary>
    public bool IsError
        => this.Kind == ProtocolFrameKind.Error;

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
