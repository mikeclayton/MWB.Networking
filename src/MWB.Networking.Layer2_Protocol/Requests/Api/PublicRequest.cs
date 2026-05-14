using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Requests.Api;

/// <summary>
/// Represents a request delivered to or initiated by the application.
/// </summary>
/// <remarks>
/// This is the application-facing representation of a protocol request,
/// containing request metadata and payload. Lifecycle and protocol
/// invariants are managed internally and are not exposed through this type.
/// </remarks>
public abstract class PublicRequest
{
    internal PublicRequest(
        uint requestId,
        uint? requestType,
        RequestActions actions,
        ReadOnlyMemory<byte> payload,
        ProtocolDirection direction)
    {
        this.RequestId = requestId;
        this.RequestType = requestType;
        this.Payload = payload;
        this.Direction = direction;
        this.Actions = actions ?? throw new ArgumentNullException(nameof(actions));
    }

    public uint RequestId
    {
        get;
    }

    public uint? RequestType
    {
        get;
    }

    public ReadOnlyMemory<byte> Payload
    {
        get;
    }

    internal ProtocolDirection Direction
    {
        get;
    }

    private protected RequestActions Actions
    {
        get;
    }
}
