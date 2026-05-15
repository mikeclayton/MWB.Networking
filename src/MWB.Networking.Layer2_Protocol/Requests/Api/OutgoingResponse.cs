using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Requests.Api;

/// <summary>
/// Represents a response sent to a remote peer.
/// </summary>
/// <remarks>
/// Created by the application when responding to an incoming request.
/// </remarks>
public sealed class OutgoingResponse : PublicResponse
{
    internal OutgoingResponse(
        RequestContext context,
        uint? responseType,
        ReadOnlyMemory<byte> payload,
        bool isError)
        : base(context, responseType, payload, isError)
    {
    }
}
