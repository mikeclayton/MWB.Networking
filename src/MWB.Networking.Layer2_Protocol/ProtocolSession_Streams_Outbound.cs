using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests;
using MWB.Networking.Layer2_Protocol.Streams;

namespace MWB.Networking.Layer2_Protocol;

public sealed partial class ProtocolSession : IProtocolSession
{
    // ------------------------------------------------------------------
    // Stream handling - Outbound
    // ------------------------------------------------------------------

    private uint NextStreamId
    {
        get;
        set;
    } = 1;

    internal IncomingStream OpenSessionStream(ReadOnlyMemory<byte> metadata)
    {
        var streamId = this.NextStreamId++;

        var context = new StreamContext(
            streamId: streamId,
            owningRequest: null  // <- session scoped
        );

        var stream = new IncomingStream(this, streamId);

        this.StreamEntries.Add(
            streamId,
            new StreamEntry(context, stream)
        );

        this.EnqueueOutboundFrame(
            ProtocolFrames.StreamOpen(streamId, metadata));

        return stream;
    }

    internal IncomingStream OpenRequestStream(RequestContext requestContext)
    {
        var streamId = this.NextStreamId++;

        var context = new StreamContext(streamId, owningRequest: requestContext);
        var stream = new IncomingStream(this, streamId);

        this.StreamEntries.Add(
            streamId,
            new StreamEntry(context, stream)
        );

        this.EnqueueOutboundFrame(
            ProtocolFrames.StreamOpen(streamId, ReadOnlyMemory<byte>.Empty));

        return stream;
    }

    internal void SendStreamData(uint streamId, ReadOnlyMemory<byte> payload)
    {
        if (!this.StreamEntries.TryGetValue(streamId, out var entry))
        {
            throw new InvalidOperationException("Unknown StreamId");
        }

        // Enforce lifecycle + request scoping
        entry.Context.EnsureOpen();

        this.EnqueueOutboundFrame(
            ProtocolFrames.StreamData(streamId, payload));
    }

    internal void CloseStream(uint streamId)
    {
        if (!this.StreamEntries.TryGetValue(streamId, out var entry))
        {
            throw new InvalidOperationException("Unknown StreamId");
        }

        // Transition lifecycle first
        entry.Context.Close();

        // Emit terminal frame
        this.EnqueueOutboundFrame(
            ProtocolFrames.StreamClose(streamId));

        // Structural cleanup
        this.RemoveStream(streamId);
    }

    internal void CloseStreamWithError(
        uint streamId,
        ReadOnlyMemory<byte> errorPayload)
    {
        if (!this.StreamEntries.TryGetValue(streamId, out var entry))
        {
            throw new InvalidOperationException("Unknown StreamId");
        }

        // Transition lifecycle first
        entry.Context.Close();

        // Emit terminal error frame
        this.EnqueueOutboundFrame(
            ProtocolFrames.StreamError(streamId, errorPayload));

        // Structural cleanup
        this.RemoveStream(streamId);
    }
}
