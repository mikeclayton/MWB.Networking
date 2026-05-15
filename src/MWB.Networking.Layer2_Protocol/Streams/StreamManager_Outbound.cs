using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Streams.Api;
using MWB.Networking.Layer2_Protocol.Streams.Lifecycle;
using MWB.Networking.Layer2_Protocol.Streams.Models;

namespace MWB.Networking.Layer2_Protocol.Streams;

internal sealed partial class StreamManager
{
    // ------------------------------------------------------------------
    // Consume: Open
    // ------------------------------------------------------------------

    internal OutgoingStream ConsumeOutgoingStreamOpen(
        uint? streamType = null,
        ReadOnlyMemory<byte> metadata = default)
    {
        var streamId = this.StreamIdProvider.AllocateOutbound();

        // create context
        var streamContext = StreamContext.CreateOutgoing(
            streamId, streamType, this.Actions);

        // register context
        this.StreamContexts.Add(streamContext);

        // transmit event
        var outgoingStream = streamContext.GetOutgoingStream();
        var streamOpened = new StreamOpenedMessage(outgoingStream, new StreamMetadata(metadata));
        this.TransmitOutgoingStreamOpen(streamOpened);

        return outgoingStream;
    }

    internal void ConsumeOutgoingStreamData(
        uint streamId,
        ReadOnlyMemory<byte> payload)
    {
        var streamContext = this.StreamContexts.GetOrThrow(streamId);

        var sessionStream = streamContext.GetSessionStream();
        var streamData = new StreamDataMessage(sessionStream, payload);
        this.TransmitOutgoingStreamData(streamData);
    }

    internal void ConsumeOutgoingStreamClose(
        uint streamId,
        ReadOnlyMemory<byte> metadata = default)
    {
        var streamContext = this.StreamContexts.GetOrThrow(streamId);

        var sessionStream = streamContext.GetSessionStream();
        var streamClosed = new StreamClosedMessage(sessionStream, new StreamMetadata(metadata));
        this.TransmitOutgoingStreamClosed(streamClosed);

        if (streamContext.IsFullyClosed)
        {
            this.RemoveStream(streamId);
        }
    }

    internal void ConsumeOutgoingStreamAbort(
        uint streamId,
        ReadOnlyMemory<byte> metadata)
    {
        var streamContext = this.StreamContexts.GetOrThrow(streamId);

        var sessionStream = streamContext.GetSessionStream();
        var streamAborted = new StreamAbortedMessage(sessionStream, new StreamMetadata(metadata));
        this.TransmitOutgoingStreamAborted(streamAborted);

        this.RemoveStream(streamId);
    }

    // ------------------------------------------------------------------
    // Transmit
    // ------------------------------------------------------------------

    internal void TransmitOutgoingStreamOpen(StreamOpenedMessage streamOpened)
    {
        ArgumentNullException.ThrowIfNull(streamOpened);

        this.Logger.LogTrace(
            "Transmitting outgoing stream open (Id={StreamId})",
            streamOpened.Stream.StreamId);

        this.Session.OutgoingActionSink.TransmitOutgoingStreamOpened(streamOpened);
    }

    internal void TransmitOutgoingStreamData(StreamDataMessage streamData)
    {
        ArgumentNullException.ThrowIfNull(streamData);

        this.Logger.LogTrace(
            "Transmitting outgoing stream data (Id={StreamId})",
            streamData.Stream.StreamId);

        this.Session.OutgoingActionSink.TransmitOutgoingStreamData(streamData);
    }

    internal void TransmitOutgoingStreamClosed(StreamClosedMessage streamClosed)
    {
        ArgumentNullException.ThrowIfNull(streamClosed);

        this.Logger.LogTrace(
            "Transmitting outgoing stream closed (Id={StreamId})",
            streamClosed.Stream.StreamId);

        this.Session.OutgoingActionSink.TransmitOutgoingStreamClosed(streamClosed);
    }

    internal void TransmitOutgoingStreamAborted(StreamAbortedMessage streamAborted)
    {
        ArgumentNullException.ThrowIfNull(streamAborted);

        this.Logger.LogTrace(
            "Transmitting outgoing stream aborted (Id={StreamId})",
            streamAborted.Stream.StreamId);

        this.Session.OutgoingActionSink.TransmitOutgoingStreamAborted(streamAborted);
    }
}
