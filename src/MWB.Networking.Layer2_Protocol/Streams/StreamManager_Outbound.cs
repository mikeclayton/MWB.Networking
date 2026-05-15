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
        var streamOpened = new OutgoingStreamOpened(outgoingStream, new StreamMetadata(metadata));
        this.TransmitOutgoingStreamOpen(streamOpened);

        return outgoingStream;
    }

    internal void ConsumeOutgoingStreamData(
        uint streamId,
        ReadOnlyMemory<byte> payload)
    {
        var streamContext = this.StreamContexts.GetOrThrow(streamId);

        var outgoingStream = streamContext.GetOutgoingStream();
        var streamData = new OutgoingStreamData(outgoingStream, payload);
        this.TransmitOutgoingStreamData(streamData);
    }

    internal void ConsumeOutgoingStreamClose(
        uint streamId,
        ReadOnlyMemory<byte> metadata = default)
    {
        var streamContext = this.StreamContexts.GetOrThrow(streamId);

        streamContext.CloseLocal();

        var outgoingStream = streamContext.GetOutgoingStream();
        var streamClosed = new OutgoingStreamClosed(outgoingStream, new StreamMetadata(metadata));
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

        streamContext.Abort();

        var outgoingStream = streamContext.GetOutgoingStream();
        var streamAborted = new OutgoingStreamAborted(outgoingStream, new StreamMetadata(metadata));
        this.TransmitOutgoingStreamAborted(streamAborted);

        this.RemoveStream(streamId);
    }

    // ------------------------------------------------------------------
    // Transmit
    // ------------------------------------------------------------------

    internal void TransmitOutgoingStreamOpen(OutgoingStreamOpened streamOpened)
    {
        ArgumentNullException.ThrowIfNull(streamOpened);

        this.Logger.LogTrace(
            "Transmitting outgoing stream open (Id={StreamId})",
            streamOpened.Stream.StreamId);

        this.Session.OutgoingActionSink.TransmitOutgoingStreamOpened(streamOpened);
    }

    internal void TransmitOutgoingStreamData(OutgoingStreamData streamData)
    {
        ArgumentNullException.ThrowIfNull(streamData);

        this.Logger.LogTrace(
            "Transmitting outgoing stream data (Id={StreamId})",
            streamData.Stream.StreamId);

        this.Session.OutgoingActionSink.TransmitOutgoingStreamData(streamData);
    }

    internal void TransmitOutgoingStreamClosed(OutgoingStreamClosed streamClosed)
    {
        ArgumentNullException.ThrowIfNull(streamClosed);

        this.Logger.LogTrace(
            "Transmitting outgoing stream closed (Id={StreamId})",
            streamClosed.Stream.StreamId);

        this.Session.OutgoingActionSink.TransmitOutgoingStreamClosed(streamClosed);
    }

    internal void TransmitOutgoingStreamAborted(OutgoingStreamAborted streamAborted)
    {
        ArgumentNullException.ThrowIfNull(streamAborted);

        this.Logger.LogTrace(
            "Transmitting outgoing stream aborted (Id={StreamId})",
            streamAborted.Stream.StreamId);

        this.Session.OutgoingActionSink.TransmitOutgoingStreamAborted(streamAborted);
    }
}
