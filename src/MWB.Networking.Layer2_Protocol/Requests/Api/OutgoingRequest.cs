using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Lifecycle.Api;

namespace MWB.Networking.Layer2_Protocol.Requests.Api;

/// <summary>
/// Represents a request initiated by the local peer.
/// Provides access to the terminal response and request-scoped streams.
/// </summary>
public sealed class OutgoingRequest
{
    internal OutgoingRequest(
        ProtocolSession session,
        RequestContext context)
    {
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    private ProtocolSession Session
    {
        get;
    }

    internal RequestContext Context
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
    public Task<ProtocolFrame> Response
        => this.Context.ResponseTask;

    /// <summary>
    /// Opens the single request-scoped outgoing stream for this request.
    /// </summary>
    public OutgoingStream OpenRequestStream(uint? streamType)
    {
        // Enforce request lifecycle rules
        this.Context.OpenStream();

        return this.Session.StreamManager.Outbound
            .OpenRequestStream(streamType, this.Context);
    }
}
