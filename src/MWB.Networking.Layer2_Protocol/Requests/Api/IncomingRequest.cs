using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Requests.Api;

/// <summary>
/// Represents a request received from a remote peer.
/// </summary>
/// <remarks>
/// Provides operations for responding to the request and opening
/// request-scoped streams.
/// </remarks>
public sealed class IncomingRequest : PublicRequest
{
    internal IncomingRequest(
        RequestContext context,
        RequestActions actions,
        ReadOnlyMemory<byte> payload)
        : base(context, actions, payload, ProtocolDirection.Incoming)
    {
    }

    /// <summary>
    /// Sends a normal (non-error) Response for this Request and closes it.
    /// </summary>
    public OutgoingResponse Respond(uint? responseType = null, ReadOnlyMemory<byte> payload = default)
    {
        // only protocol violations can set isError:true.
        // *application* errors should be indicated by setting an appropriate responseType and payload.
        return this.Actions.Respond(this.Context, responseType, payload, isError:false);
    }
}
