using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Requests.Api;

/// <summary>
/// Represents a request initiated by the local application.
/// </summary>
/// <remarks>
/// Provides a task that completes when the corresponding response is received.
/// </remarks>
public sealed class OutgoingRequest : PublicRequest
{
    internal OutgoingRequest(
        uint requestId,
        uint? requestType,
        RequestActions actions,
        ReadOnlyMemory<byte> payload)
        : base(requestId, requestType, actions, payload, ProtocolDirection.Outgoing)
    {
    }

    /// <summary>
    /// Task that completes when the terminal response or error
    /// frame is received for this request.
    /// </summary>
    public Task<IncomingResponse> Response
        => this.Context.ResponseTask;
}
