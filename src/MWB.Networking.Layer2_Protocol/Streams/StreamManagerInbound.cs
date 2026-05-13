using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Frames;
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
        StreamContexts streamContexts)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.StreamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
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

    private StreamContexts StreamContexts
    {
        get;
    }

    // ------------------------------------------------------------------
    // Utility methods
    // ------------------------------------------------------------------

#pragma warning disable CA1822 // Mark members as static
    // this *could* be static but it reads better at call sites if it's an instance method
    private void EnsureIsIncomingStream(
#pragma warning restore CA1822 // Mark members as static
        StreamContext streamContext,
        out IncomingStream incomingStream)
    {
        streamContext.EnsureIncoming();
        incomingStream = streamContext.GetIncomingStreamOrThrow();
    }

    // ------------------------------------------------------------------
    // Stream handling - Inbound
    // ------------------------------------------------------------------

    internal void AbortIncomingStream(uint streamId)
    {
        if (!this.StreamContexts.TryGet(streamId, out var streamContext))
        {
            return;
        }

        streamContext.Close();

        // peer-owned stream aborted by local peer
        // so notify the remote peer to abort the stream as well
        this.Session.SendOutboundFrame(
            ProtocolFrames.StreamAbort(streamId));

        this.StreamManager.RemoveStream(streamId);
    }

    // ------------------------------------------------------------------
    // Consume: Open
    // ------------------------------------------------------------------

    private void ConsumeIncomingStreamOpen(
        uint streamId,
        uint? streamType,
        ReadOnlyMemory<byte> payload)
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
        var streamContext = new StreamContext(
            streamId,
            streamType,
            ProtocolDirection.Incoming);

        // create stream entity (no payload!)
        var incomingStream = new IncomingStream(
            streamContext,
            payload);

        // Attach stream to context (critical for identity consistency)
        streamContext.AttachIncomingStream(incomingStream);

        // Register context
        this.StreamContexts.Add(streamContext);

        // Publish open event
        this.PublishIncomingStreamOpened(incomingStream, payload);
    }

    // ------------------------------------------------------------------
    // Consume: Data
    // ------------------------------------------------------------------

    private void ConsumeIncomingStreamData(
        uint streamId,
        ReadOnlyMemory<byte> payload)
    {
        var streamContext = this.StreamContexts.GetOrThrow(streamId);

        this.EnsureIsIncomingStream(streamContext, out var incomingStream);

        streamContext.EnsureOpen();

        this.PublishIncomingStreamData(incomingStream, payload);
    }

    // ------------------------------------------------------------------
    // Consume: Close
    // ------------------------------------------------------------------

    private void ConsumeIncomingStreamClose(
        uint streamId,
        ReadOnlyMemory<byte> payload)
    {
        var streamContext = this.StreamContexts.GetOrThrow(streamId);

        this.EnsureIsIncomingStream(streamContext, out var incomingStream);

        // Transition state first
        streamContext.Close();
        incomingStream.Close();

        // Publish while still registered
        this.PublishIncomingStreamClosed(incomingStream, payload);

        // Then remove from manager
        this.StreamManager.RemoveStream(streamId);
    }

    // ------------------------------------------------------------------
    // Consume: Abort
    // ------------------------------------------------------------------

    private void ConsumeIncomingStreamAbort(
        uint streamId,
        ReadOnlyMemory<byte> payload)
    {
        var streamContext = this.StreamContexts.GetOrThrow(streamId);

        this.EnsureIsIncomingStream(streamContext, out var incomingStream);

        // Transition state first
        streamContext.Close();
        incomingStream.Close();

        // Publish while still registered
        this.PublishIncomingStreamAborted(incomingStream, payload);

        // Then remove
        this.StreamManager.RemoveStream(streamId);
    }

    // ------------------------------------------------------------------
    // Publish helpers
    // ------------------------------------------------------------------

    private void PublishIncomingStreamOpened(
        IncomingStream stream,
        ReadOnlyMemory<byte> metadata)
    {
        ArgumentNullException.ThrowIfNull(stream);

        this.Logger.LogTrace(
            "Publishing incoming stream open (Id={StreamId})",
            stream.StreamId);

        var streamOpened = new IncomingStreamOpened(
            stream,
            new StreamMetadata(metadata));

        this.Session.IncomingActionSink.PublishIncomingStreamOpened(streamOpened);
    }

    private void PublishIncomingStreamData(
        IncomingStream stream,
        ReadOnlyMemory<byte> payload)
    {
        ArgumentNullException.ThrowIfNull(stream);

        this.Logger.LogTrace(
            "Publishing incoming stream data (Id={StreamId})",
            stream.StreamId);

        var streamData = new IncomingStreamData(
            stream,
            payload);

        this.Session.IncomingActionSink.PublishIncomingStreamData(streamData);
    }

    private void PublishIncomingStreamClosed(
        IncomingStream stream,
        ReadOnlyMemory<byte> metadata)
    {
        ArgumentNullException.ThrowIfNull(stream);

        this.Logger.LogTrace(
            "Publishing incoming stream close (Id={StreamId})",
            stream.StreamId);

        var streamClosed = new IncomingStreamClosed(
            stream,
            new StreamMetadata(metadata));

        this.Session.IncomingActionSink.PublishIncomingStreamClosed(streamClosed);
    }

    private void PublishIncomingStreamAborted(
        IncomingStream stream,
        ReadOnlyMemory<byte> metadata)
    {
        ArgumentNullException.ThrowIfNull(stream);

        this.Logger.LogTrace(
            "Publishing incoming stream abort (Id={StreamId})",
            stream.StreamId);

        var streamAborted = new IncomingStreamAborted(
            stream,
            new StreamMetadata(metadata));

        this.Session.IncomingActionSink.PublishIncomingStreamAborted(streamAborted);
    }
}
