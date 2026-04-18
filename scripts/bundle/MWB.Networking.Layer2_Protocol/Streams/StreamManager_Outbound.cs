using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;
using MWB.Networking.Layer2_Protocol.Streams.Api;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;
using MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Streams;

public sealed partial class StreamManager
{
    // ------------------------------------------------------------------
    // Stream handling - Outbound
    // ------------------------------------------------------------------

    internal OutgoingStream OpenRequestStream(RequestContext owningRequest)
    {
        ArgumentNullException.ThrowIfNull(owningRequest);

        // Allocate outbound stream ID
        var streamId = this.StreamIdProvider.AllocateOutbound();

        // Create request-scoped stream context
        var context = new StreamContext(
            streamId: streamId,
            owningRequest: owningRequest
        );

        // Outgoing stream (locally initiated)
        var stream = new OutgoingStream(this.Session, streamId);

        // Track stream
        this.StreamEntries.Add(
            streamId,
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

    internal OutgoingStream OpenSessionStream(ReadOnlyMemory<byte> metadata)
    {
        var streamId = this.StreamIdProvider.AllocateOutbound();

        var context = new StreamContext(
            streamId: streamId,
            owningRequest: null  // session scoped
        );

        var stream = new OutgoingStream(this.Session, streamId);

        this.StreamEntries.Add(
            streamId,
            new StreamEntry(context, stream)
        );

        this.Session.EnqueueOutboundFrame(
            ProtocolFrames.StreamOpen(streamId, metadata));

        return stream;
    }

    internal void CloseOutgoingStream(uint streamId)
    {
        if (!this.StreamEntries.TryGetValue(streamId, out var entry))
        {
            return;
        }

        entry.Context.Close();

        this.Session.EnqueueOutboundFrame(
            ProtocolFrames.StreamClose(streamId));

        RemoveStream(streamId);
    }

    internal void AbortStream(uint streamId)
    {
        if (!this.StreamEntries.TryGetValue(streamId, out var entry))
        {
            return;
        }

        entry.Context.Close();

        this.Session.EnqueueOutboundFrame(
            ProtocolFrames.StreamAbort(streamId));

        this.RemoveStream(streamId);
    }
}
