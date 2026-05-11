using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Streams.Api;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;
using MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Streams;

internal sealed class StreamManagerOutbound
{
    internal StreamManagerOutbound(
        ProtocolSession session,
        StreamManager streamManager,
        StreamEntries streamEntries,
        OddEvenStreamIdProvider streamIdProvider)
    {
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.StreamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        this.StreamEntries = streamEntries ?? throw new ArgumentNullException(nameof(streamEntries));
        this.StreamIdProvider = streamIdProvider ?? throw new ArgumentNullException(nameof(streamIdProvider));
    }

    private ProtocolSession Session
    {
        get;
    }

    private StreamManager StreamManager
    {
        get;
    }

    private StreamEntries StreamEntries
    {
        get;
    }

    private OddEvenStreamIdProvider StreamIdProvider
    {
        get;
    }

    // ------------------------------------------------------------------
    // Stream handling - Outbound
    // ------------------------------------------------------------------

    internal OutgoingStream OpenRequestStream(uint? streamType, RequestContext owningRequest)
    {
        ArgumentNullException.ThrowIfNull(owningRequest);

        // Allocate outbound stream ID
        var streamId = this.StreamIdProvider.AllocateOutbound();

        // Create request-scoped stream context
        var context = new StreamContext(
            streamId: streamId,
            streamType: streamType,
            owningRequest: owningRequest // request-scoped
        );

        // Outgoing stream (locally initiated)
        var stream = new OutgoingStream(this.Session, context, streamId);

        // Track stream
        this.StreamEntries.AddStreamEntry(
            new StreamEntry(context, stream)
        );

        // Emit protocol frame (request-scoped)
        this.Session.SendOutboundFrame(
            ProtocolFrames.StreamOpen(
                streamId,
                streamType: streamType,
                requestId: owningRequest.RequestId
            )
        );

        return stream;
    }

    internal OutgoingStream OpenSessionStream(uint? streamType = null, ReadOnlyMemory<byte> metadata = default)
    {
        var streamId = this.StreamIdProvider.AllocateOutbound();

        var context = new StreamContext(
            streamId: streamId,
            streamType: streamType,
            owningRequest: null  // session scoped
        );

        var stream = new OutgoingStream(this.Session, context, streamId);

        this.StreamEntries.AddStreamEntry(
            new StreamEntry(context, stream)
        );

        this.Session.SendOutboundFrame(
            ProtocolFrames.StreamOpen(streamId, streamType: streamType, metadata: metadata));

        return stream;
    }

    internal void CloseOutgoingStream(uint streamId)
    {
        if (!this.StreamEntries.TryGetStreamEntry(streamId, out var entry))
        {
            return;
        }

        entry.Context.Close();

        this.Session.SendOutboundFrame(
            ProtocolFrames.StreamClose(streamId));

        this.StreamManager.RemoveStream(streamId);
    }

    internal void AbortStream(uint streamId)
    {
        if (!this.StreamEntries.TryGetStreamEntry(streamId, out var entry))
        {
            return;
        }

        entry.Context.Close();

        // locally-owned stream aborted by local peer
        // so notify the remote peer to abort the stream as well
        this.Session.SendOutboundFrame(
            ProtocolFrames.StreamAbort(streamId));

        this.StreamManager.RemoveStream(streamId);
    }
}
