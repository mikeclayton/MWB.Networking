using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Streams.Api;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;
using MWB.Networking.Layer2_Protocol.Streams.Lifecycle;
using MWB.Networking.Layer2_Protocol.Streams.Publish;

namespace MWB.Networking.Layer2_Protocol.Streams;

internal sealed class StreamManagerOutbound
{
    internal StreamManagerOutbound(
        ILogger logger,
        ProtocolSession session,
        StreamManager streamManager,
        StreamActions actions,
        StreamContexts streamContexts,
        OddEvenStreamIdProvider streamIdProvider)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.StreamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        this.Actions = actions ?? throw new ArgumentNullException(nameof(actions));
        this.StreamContexts = streamContexts ?? throw new ArgumentNullException(nameof(streamContexts));
        this.StreamIdProvider = streamIdProvider ?? throw new ArgumentNullException(nameof(streamIdProvider));
    }

    private ILogger Logger
    {
        get;
    }

    private ProtocolSession Session
    {
        get;
    }

    private StreamManager StreamManager
    {
        get;
    }

    private StreamActions Actions
    {
        get;
    }

    private StreamContexts StreamContexts
    {
        get;
    }

    private OddEvenStreamIdProvider StreamIdProvider
    {
        get;
    }

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
        this.Session.OutgoingActionSink.TransmitOutgoingStreamData(streamData);
    }

    internal void ConsumeOutgoingStreamClose(
        uint streamId,
        ReadOnlyMemory<byte> metadata = default)
    {
        var streamContext = this.StreamContexts.GetOrThrow(streamId);

        streamContext.CloseLocal();

        var outgoingStream = streamContext.GetOutgoingStream();
        var streamClosed = new OutgoingStreamClosed(outgoingStream, new StreamMetadata(metadata));
        this.Session.OutgoingActionSink.TransmitOutgoingStreamClosed(streamClosed);

        if (streamContext.IsFullyClosed)
        {
            this.StreamManager.RemoveStream(streamId);
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
        this.Session.OutgoingActionSink.TransmitOutgoingStreamAborted(streamAborted);

        this.StreamManager.RemoveStream(streamId);
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

    internal void TransmitOutgoingStreamOpen(OutgoingStreamAborted streamAborted)
    {
        ArgumentNullException.ThrowIfNull(streamAborted);

        this.Logger.LogTrace(
            "Transmitting outgoing stream open (Id={StreamId})",
            streamAborted.Stream.StreamId);

        this.Session.OutgoingActionSink.TransmitOutgoingStreamAborted(streamAborted);
    }
}
