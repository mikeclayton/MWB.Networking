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
        RequestContext context,
        RequestActions actions,
        ReadOnlyMemory<byte> payload,
        ProtocolDirection direction)
    {
        this.Context = context ?? throw new ArgumentNullException(nameof(context));
        this.Actions = actions ?? throw new ArgumentNullException(nameof(actions));
        this.Payload = payload;
        this.Direction = direction;
    }

    private protected RequestContext Context
    {
        get;
    }

    private protected RequestActions Actions
    {
        get;
    }

    public uint RequestId
        => this.Context.RequestId;

    public uint? RequestType
        => this.Context.RequestType;

    public ReadOnlyMemory<byte> Payload
    {
        get;
    }

    internal ProtocolDirection Direction
    {
        get;
    }

    /// <summary>
    /// Opens the single Request-scoped Stream for this Request.
    /// </summary>
    public SessionStream OpenRequestStream(uint? streamType)
    {
        return this.Actions.OpenRequestStream(this.Context, streamType);
    }
}
