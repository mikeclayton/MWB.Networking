using MWB.Networking.Layer2_Protocol.Internal;

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
        uint requestId,
        uint? responseType,
        ReadOnlyMemory<byte> payload,
        bool isError)
        : base(requestId, responseType, payload, isError, ProtocolDirection.Outgoing)
    {
    }
}
