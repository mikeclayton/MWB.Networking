using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Streams.Api;
using MWB.Networking.Layer2_Protocol.Streams.Lifecycle;
using MWB.Networking.Layer2_Protocol.Streams.Models;

namespace MWB.Networking.Layer2_Protocol.Streams;

internal sealed partial class StreamManager
{

    // ------------------------------------------------------------------
    // Consume: Open
    // ------------------------------------------------------------------

    internal void ConsumeIncomingStreamOpen(
        uint streamId,
        uint? streamType,
        ReadOnlyMemory<byte> metadata)
    {
        this.StreamContexts.ThrowIfExists(streamId);

        // Enforce the odd/even parity contract: the peer must use IDs of the
        // opposite parity to our outbound IDs. Accepting same-parity IDs would
        // guarantee a collision when we next allocate an outbound stream ID.
        if (!this.IsValidInboundStreamId(streamId))
        {
            throw ProtocolException.ProtocolViolation(
                $"StreamId {streamId} has the wrong parity for an inbound stream.");
        }

        // create context
        var streamContext = StreamContext.CreateIncoming(
            streamId, streamType, this.Actions);

        // register context
        this.StreamContexts.Add(streamContext);

        // publish event
        var incomingStream = streamContext.GetIncomingStream();
        var streamOpened = new StreamOpenedMessage(incomingStream, new StreamMetadata(metadata));
        this.PublishIncomingStreamOpened(streamOpened);
    }

    // ------------------------------------------------------------------
    // Consume: Data
    // ------------------------------------------------------------------

    internal void ConsumeIncomingStreamData(
        uint streamId,
        ReadOnlyMemory<byte> payload)
    {
        var streamContext = this.StreamContexts.GetOrThrow(streamId);
        streamContext.EnsureCanReceive();

        var incomingStream = streamContext.GetIncomingStream();

        // Publish data event
        var streamData = new StreamDataMessage(incomingStream, payload);
        this.PublishIncomingStreamData(streamData);
    }

    // ------------------------------------------------------------------
    // Consume: Close
    // ------------------------------------------------------------------

    internal void ConsumeIncomingStreamClose(
        uint streamId,
        ReadOnlyMemory<byte> metadata)
    {
        var streamContext = this.StreamContexts.GetOrThrow(streamId);
        var incomingStream = streamContext.GetIncomingStream();

        // Transition state first
        streamContext.CloseRemote();

        // Publish while still registered
        var streamClosed = new StreamClosedMessage(incomingStream, new StreamMetadata(metadata));
        this.PublishIncomingStreamClosed(streamClosed);

        // only remove when both halves are done
        if (streamContext.IsFullyClosed)
        {
            this.RemoveStream(streamId);
        }
    }

    // ------------------------------------------------------------------
    // Consume: Abort
    // ------------------------------------------------------------------

    internal void ConsumeIncomingStreamAbort(
        uint streamId,
        ReadOnlyMemory<byte> metadata)
    {
        var streamContext = this.StreamContexts.GetOrThrow(streamId);
        var incomingStream = streamContext.GetIncomingStream();

        streamContext.Abort();

        // The remote peer sent a StreamAbort frame.
        // tear down local state and notify the application.
        var streamAborted = new StreamAbortedMessage(incomingStream, new StreamMetadata(metadata));
        this.PublishIncomingStreamAborted(streamAborted);

        this.RemoveStream(streamId);
    }

    // ------------------------------------------------------------------
    // Publish
    // ------------------------------------------------------------------

    internal void PublishIncomingStreamOpened(
        StreamOpenedMessage streamOpened)
    {
        ArgumentNullException.ThrowIfNull(streamOpened);

        this.Logger.LogTrace(
            "Publishing incoming stream open (Id={StreamId})",
            streamOpened.Stream.StreamId);

        this.Session.IncomingActionSink.PublishIncomingStreamOpened(streamOpened);
    }

    internal void PublishIncomingStreamData(
        StreamDataMessage streamData)
    {
        ArgumentNullException.ThrowIfNull(streamData);

        this.Logger.LogTrace(
            "Publishing incoming stream data (Id={StreamId})",
            streamData.Stream.StreamId);

        this.Session.IncomingActionSink.PublishIncomingStreamData(streamData);
    }

    internal void PublishIncomingStreamClosed(
        StreamClosedMessage streamClosed)
    {
        ArgumentNullException.ThrowIfNull(streamClosed);

        this.Logger.LogTrace(
            "Publishing incoming stream close (Id={StreamId})",
            streamClosed.Stream.StreamId);

        this.Session.IncomingActionSink.PublishIncomingStreamClosed(streamClosed);
    }

    internal void PublishIncomingStreamAborted(
        StreamAbortedMessage streamAborted)
    {
        ArgumentNullException.ThrowIfNull(streamAborted);

        this.Logger.LogTrace(
            "Publishing incoming stream abort (Id={StreamId})",
            streamAborted.Stream.StreamId);

        this.Session.IncomingActionSink.PublishIncomingStreamAborted(streamAborted);
    }
}
