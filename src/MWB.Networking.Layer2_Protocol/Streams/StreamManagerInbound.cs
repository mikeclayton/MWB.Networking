using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Lifecycle.Api;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Streams;

internal sealed class StreamManagerInbound
{
    internal StreamManagerInbound(ProtocolSession session, StreamManager streamManager, StreamEntries streamEntries)
    {
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.StreamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        this.StreamEntries = streamEntries ?? throw new ArgumentNullException(nameof(streamEntries));
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

    // ------------------------------------------------------------------
    // Utility methods
    // ------------------------------------------------------------------

    private void EnsureFrameHasStreamId(
        ProtocolFrame frame,
        out uint streamId)
    {
        streamId = frame.StreamId
            ?? throw ProtocolException.ProtocolViolation(
                frame,
                $"{nameof(ProtocolFrame)} with {nameof(ProtocolFrame.Kind)} of {nameof(ProtocolFrameKind)} must have a {nameof(ProtocolFrame.StreamId)}");
    }

    private void EnsureStreamEntryDoesNotExist(
        ProtocolFrame frame,
        uint streamId)
    {
        if (this.StreamEntries.StreamEntryExists(streamId))
        {
            throw ProtocolException.InvalidFrameSequence(
                frame, "Duplicate StreamId");
        }
    }

    private void EnsureStreamEntryExists(
        ProtocolFrame frame,
        uint streamId,
        out StreamEntry streamEntry)
    {
        if (this.StreamEntries.TryGetStreamEntry(streamId, out var result))
        {
            streamEntry = result;
            return;
        }
        throw ProtocolException.InvalidFrameSequence(
            frame, "Unknown StreamId");
    }

#pragma warning disable CA1822 // Mark members as static
    // this *could* be static but it reads better at call sites if it's an instance method
    private void EnsureIsIncomingStream(
#pragma warning restore CA1822 // Mark members as static
        ProtocolFrame frame,
        StreamEntry streamEntry,
        out IncomingStream incomingStream)
    {
        if (!streamEntry.IsIncoming)
        {
            throw ProtocolException.ProtocolViolation(
                frame, "Inbound stream frames may only target streams opened by the peer.");
        }
        incomingStream = streamEntry.GetIncomingStreamOrThrow();
    }

    // ------------------------------------------------------------------
    // Stream handling - Inbound
    // ------------------------------------------------------------------

    internal void AbortIncomingStream(uint streamId)
    {
        if (!this.StreamEntries.TryGetStreamEntry(streamId, out var entry))
        {
            return;
        }

        entry.Context.Close();

        // peer-owned stream aborted by local peer
        // so notify the remote peer to abort the stream as well
        this.Session.EnqueueOutboundFrame(
            ProtocolFrames.StreamAbort(streamId));

        this.StreamManager.RemoveStream(streamId);
    }

    internal void ProcessStreamFrame(ProtocolFrame frame)
    {
        switch (frame.Kind)
        {
            case ProtocolFrameKind.StreamOpen:
                this.ProcessIncomingStreamOpenFrame(frame);
                return;
            case ProtocolFrameKind.StreamData:
                this.ProcessIncomingStreamDataFrame(frame);
                return;
            case ProtocolFrameKind.StreamClose:
                this.ProcessIncomingStreamCloseFrame(frame);
                break;
            default:
                throw ProtocolException.InvalidFrameSequence(frame, "Invalid stream frame kind");
        }
    }

    private void ProcessIncomingStreamOpenFrame(ProtocolFrame frame)
    {
        // validate the frame
        this.EnsureFrameHasStreamId(frame, out var streamId);
        this.EnsureStreamEntryDoesNotExist(frame, streamId);

        // if the frame is associated with a request, make sure the request exists and get its context
        RequestContext? owningRequestContext = null;
        if (frame.RequestId is not null)
        {
            if (!this.Session.RequestManager.TryGetRequestContext(frame.RequestId.Value, out owningRequestContext))
            {
                throw ProtocolException.InvalidFrameSequence(frame, "Unknown RequestId for StreamOpen");
            }
        }

        // build the context and create the IncomingStream instance
        var streamType = frame.StreamType;
        var streamContext = new StreamContext(streamId, streamType, owningRequestContext);
        var incomingStream = new IncomingStream(this.Session, streamContext);

        // add a new stream entry into the entry cache
        var streamEntry = new StreamEntry(
            streamContext,
            incomingStream
        );
        this.StreamEntries.AddStreamEntry(streamEntry);

        // semantic notification
        this.Session.OnStreamOpened(
            incomingStream,
            new StreamMetadata(frame.Payload));
    }

    private void ProcessIncomingStreamDataFrame(ProtocolFrame frame)
    {
        // validate the frame
        this.EnsureFrameHasStreamId(frame, out var streamId);
        this.EnsureStreamEntryExists(frame, streamId, out var streamEntry);
        this.EnsureIsIncomingStream(frame, streamEntry, out var incomingStream);

        streamEntry.Context.EnsureOpen();
        this.Session.OnStreamDataReceived(incomingStream, frame.Payload);
    }

    private void ProcessIncomingStreamCloseFrame(ProtocolFrame frame)
    {
        // validate the frame
        this.EnsureFrameHasStreamId(frame, out var streamId);
        this.EnsureStreamEntryExists(frame, streamId, out var streamEntry);
        this.EnsureIsIncomingStream(frame, streamEntry, out var incomingStream);

        // close the stream context
        streamEntry.Context.Close();
        // mark the IncomingStream as closed by peer
        incomingStream.Close();
        this.Session.OnStreamClosed(incomingStream, new StreamMetadata(frame.Payload));

        this.StreamManager.RemoveStream(streamId);
    }
}
