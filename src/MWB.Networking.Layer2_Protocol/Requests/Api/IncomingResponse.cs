using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Requests.Api;

/// <summary>
/// Represents a response delivered to or emitted by the application.
/// </summary>
/// <remarks>
/// Encapsulates the response metadata and payload associated with a request.
/// Responses are immutable data objects and do not participate in request
/// lifecycle or protocol invariant enforcement.
/// </remarks>
public sealed class IncomingResponse : PublicResponse
{
    internal IncomingResponse(
        RequestContext context,
        uint? responseType,
        ReadOnlyMemory<byte> payload,
        bool isError)
        : base(context, responseType, payload, isError)
    {
    }
}
