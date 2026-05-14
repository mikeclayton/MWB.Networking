using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Streams.Api;
using MWB.Networking.Layer2_Protocol.Streams.Lifecycle;
using MWB.Networking.Layer2_Protocol.Streams.Publish;

namespace MWB.Networking.Layer2_Protocol.Streams;

internal sealed class StreamManagerInbound
{
    internal StreamManagerInbound(
        ILogger logger,
        ProtocolSession session,
        StreamManager streamManager,
        StreamActions actions,
        StreamContexts streamContexts)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.StreamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        this.Actions = actions ?? throw new ArgumentNullException(nameof(actions));
        this.StreamContexts = streamContexts ?? throw new ArgumentNullException(nameof(streamContexts));
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
        if (!this.StreamManager.IsValidInboundStreamId(streamId))
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
        var streamOpened = new IncomingStreamOpened(incomingStream, new StreamMetadata(metadata));
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
        var incomingStream = streamContext.GetIncomingStream();
        streamContext.EnsureCanReceive();

        // Publish data event
        var streamData = new IncomingStreamData(incomingStream, payload);
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
        var streamClosed = new IncomingStreamClosed(incomingStream, new StreamMetadata(metadata));
        this.PublishIncomingStreamClosed(streamClosed);

        // Then remove from manager
        this.StreamManager.RemoveStream(streamId);
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
        incomingStream.Abort();

        // peer-owned stream aborted by local peer
        // so notify the remote peer to abort the stream as well
        var streamAborted = new IncomingStreamAborted(incomingStream, new StreamMetadata(metadata));
        this.PublishIncomingStreamAborted(streamAborted);

        this.StreamManager.RemoveStream(streamId);
    }

    // ------------------------------------------------------------------
    // Publish
    // ------------------------------------------------------------------

    internal void PublishIncomingStreamOpened(
        IncomingStreamOpened streamOpened)
    {
        ArgumentNullException.ThrowIfNull(streamOpened);

        this.Logger.LogTrace(
            "Publishing incoming stream open (Id={StreamId})",
            streamOpened.Stream.StreamId);

        this.Session.IncomingActionSink.PublishIncomingStreamOpened(streamOpened);
    }

    internal void PublishIncomingStreamData(
        IncomingStreamData streamData)
    {
        ArgumentNullException.ThrowIfNull(streamData);

        this.Logger.LogTrace(
            "Publishing incoming stream data (Id={StreamId})",
            streamData.Stream.StreamId);

        this.Session.IncomingActionSink.PublishIncomingStreamData(streamData);
    }

    internal void PublishIncomingStreamClosed(
        IncomingStreamClosed streamClosed)
    {
        ArgumentNullException.ThrowIfNull(streamClosed);

        this.Logger.LogTrace(
            "Publishing incoming stream close (Id={StreamId})",
            streamClosed.Stream.StreamId);

        this.Session.IncomingActionSink.PublishIncomingStreamClosed(streamClosed);
    }

    internal void PublishIncomingStreamAborted(
        IncomingStreamAborted streamAborted)
    {
        ArgumentNullException.ThrowIfNull(streamAborted);

        this.Logger.LogTrace(
            "Publishing incoming stream abort (Id={StreamId})",
            streamAborted.Stream.StreamId);

        this.Session.IncomingActionSink.PublishIncomingStreamAborted(streamAborted);
    }
}
