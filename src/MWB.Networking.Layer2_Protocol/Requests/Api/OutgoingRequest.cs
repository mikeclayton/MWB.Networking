using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;
using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Requests.Api;

/// <summary>
/// Represents a request initiated by the local peer.
/// Provides access to the terminal response and request-scoped streams.
/// </summary>
public sealed class OutgoingRequest
{
    internal OutgoingRequest(
        RequestManager requestManager,
        RequestContext context)
    {
        this.RequestManager = requestManager ?? throw new ArgumentNullException(nameof(requestManager));
        this.Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    private RequestManager RequestManager
    {
        get;
    }

    private RequestContext Context
    {
        get;
    }

    /// <summary>
    /// The protocol RequestId for this request.
    /// </summary>
    public uint RequestId
        => this.Context.RequestId;

    /// <summary>
    /// Task that completes when the terminal response or error
    /// frame is received for this request.
    /// </summary>
    public Task<IncomingResponse> Response
        => this.Context.ResponseTask;

    /// <summary>
    /// Opens the single request-scoped outgoing stream for this request.
    /// </summary>
    public OutgoingStream OpenRequestStream(uint? streamType)
    {
        return this.RequestManager.Actions.OpenRequestStream(this.Context, streamType);
    }
}
