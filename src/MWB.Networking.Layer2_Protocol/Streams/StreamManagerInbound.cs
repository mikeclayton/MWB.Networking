using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Streams.Api;
using MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Streams;

internal sealed class StreamManagerInbound
{
    internal StreamManagerInbound(ProtocolSession session, StreamManager streamManager, StreamContexts streamContexts)
    {
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.StreamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        this.StreamContexts = streamContexts ?? throw new ArgumentNullException(nameof(streamContexts));
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

    private void EnsureStreamEntryDoesNotExist(
        ProtocolFrame frame,
        uint streamId)
    {
        if (this.StreamContexts.Exists(streamId))
        {
            throw ProtocolException.InvalidSequence(
                "Duplicate StreamId");
        }
    }

    private void EnsureStreamContextExists(
        uint streamId,
        out StreamContext streamEntry)
    {
        if (this.StreamContexts.TryGet(streamId, out var result))
        {
            streamEntry = result;
            return;
        }
        throw ProtocolException.InvalidSequence(
            "Unknown StreamId");
    }

#pragma warning disable CA1822 // Mark members as static
    // this *could* be static but it reads better at call sites if it's an instance method
    private void EnsureIsIncomingStream(
#pragma warning restore CA1822 // Mark members as static
        StreamContext streamContext,
        out IncomingStream incomingStream)
    {
        if (!streamContext.IsIncoming)
        {
            throw ProtocolException.ProtocolViolation(
                "Inbound stream frames may only target streams opened by the peer.");
        }
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
            case ProtocolFrameKind.StreamAbort:
                this.ProcessIncomingStreamAbortFrame(frame);
                break;
            default:
                throw ProtocolException.InvalidSequence("Invalid stream frame kind");
        }
    }

    private void ProcessIncomingStreamOpenFrame(ProtocolFrame frame)
    {
        // validate the frame
        this.EnsureStreamEntryDoesNotExist(frame, streamId);

        // Enforce the odd/even parity contract: the peer must use IDs of the
        // opposite parity to our outbound IDs. Accepting same-parity IDs would
        // guarantee a collision when we next allocate an outbound stream ID.
        if (!this.StreamManager.IsValidInboundStreamId(streamId))
        {
            throw ProtocolException.ProtocolViolation(
                $"StreamId {streamId} has the wrong parity for an inbound stream.");
        }

        // build the context and create the IncomingStream instance
        var streamType = frame.StreamType;
        var streamContext = new StreamContext(streamId, streamType);
        var incomingStream = new IncomingStream(this.Session, streamContext);

        // add a new stream context into the cache
        this.StreamContexts.Add(streamContext);

        // semantic notification
        this.Session.OnStreamOpened(
            incomingStream,
            new StreamMetadata(frame.Payload));
    }

    private void ProcessIncomingStreamDataFrame(ProtocolFrame frame)
    {
        // validate the frame
        this.EnsureStreamContextExists(streamId, out var streamContext);
        this.EnsureIsIncomingStream(streamContext, out var incomingStream);

        streamContext.EnsureOpen();
        this.Session.OnStreamDataReceived(incomingStream, frame.Payload);
    }

    private void ProcessIncomingStreamCloseFrame(ProtocolFrame frame)
    {
        // validate the frame
        this.EnsureStreamContextExists(streamId, out var streamContext);
        this.EnsureIsIncomingStream(streamContext, out var incomingStream);

        // close the stream context
        streamContext.Close();
        // mark the IncomingStream as closed by peer
        incomingStream.Close();
        this.Session.OnStreamClosed(incomingStream, new StreamMetadata(frame.Payload));

        this.StreamManager.RemoveStream(streamId);
    }

    private void ProcessIncomingStreamAbortFrame(ProtocolFrame frame)
    {
        this.EnsureStreamContextExists(frame, streamId, out var streamContext);
        this.EnsureIsIncomingStream(frame, streamContext, out var incomingStream);

        streamContext.Close();
        this.StreamManager.RemoveStream(streamId);

        this.Session.OnStreamAborted(incomingStream, new StreamMetadata(frame.Payload));
    }
}
