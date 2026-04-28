using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Lifecycle.Api;
using MWB.Networking.Layer2_Protocol.Lifecycle.Infrastructure;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;
using MWB.Networking.Layer2_Protocol.Session;
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
        var stream = new OutgoingStream(this.Session, streamId);

        // Track stream
        this.StreamEntries.AddStreamEntry(
            new StreamEntry(context, stream)
        );

        // Emit protocol frame (request-scoped)
        this.Session.EnqueueOutboundFrame(
            ProtocolFrames.StreamOpen(
                streamId,
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

        var stream = new OutgoingStream(this.Session, streamId);

        this.StreamEntries.AddStreamEntry(
            new StreamEntry(context, stream)
        );

        this.Session.EnqueueOutboundFrame(
            ProtocolFrames.StreamOpen(streamId, metadata: metadata));

        return stream;
    }

    internal void CloseOutgoingStream(uint streamId)
    {
        if (!this.StreamEntries.TryGetStreamEntry(streamId, out var entry))
        {
            return;
        }

        entry.Context.Close();

        this.Session.EnqueueOutboundFrame(
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

        this.Session.EnqueueOutboundFrame(
            ProtocolFrames.StreamAbort(streamId));

        this.StreamManager.RemoveStream(streamId);
    }
}
