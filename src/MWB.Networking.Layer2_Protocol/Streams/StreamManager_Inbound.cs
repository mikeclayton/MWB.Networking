using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;
using MWB.Networking.Layer2_Protocol.Streams.Api;
using MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Streams;

public sealed partial class StreamManager
{
    // ------------------------------------------------------------------
    // Stream handling - Inbound
    // ------------------------------------------------------------------

    internal void AbortIncomingStream(uint streamId)
    {
        if (!this.TryGetStreamEntry(streamId, out var entry))
        {
            return;
        }

        entry.Context.Close();

        // Peer-owned stream aborted by local peer
        this.Session.EnqueueOutboundFrame(
            ProtocolFrames.StreamAbort(streamId));

        this.RemoveStream(streamId);
    }

    internal void ProcessInboundStreamFrame(ProtocolFrame frame)
    {
        if (frame.StreamId is null)
        {
            throw ProtocolException.InvalidFrameSequence(frame, "Stream-related frame missing StreamId");
        }

        var streamId = frame.StreamId.Value;
        _ = this.TryGetStreamEntry(streamId, out var streamEntry);
        var incomingStream = streamEntry?.IncomingStream;

        if (frame.Kind == ProtocolFrameKind.StreamOpen)
        {
            if (streamEntry is not null)
            {
                throw ProtocolException.InvalidFrameSequence(frame, "Duplicate StreamId");
            }

            RequestContext? owningRequestContext = null;
            if (frame.RequestId is not null)
            {
                if (!this.Session.RequestManager.TryGetRequestContext(frame.RequestId.Value, out owningRequestContext))
                {
                    throw ProtocolException.InvalidFrameSequence(frame, "Unknown RequestId for StreamOpen");
                }
            }

            IncomingRequest? incomingRequest =
                owningRequestContext is not null
                    ? new IncomingRequest(this.Session, owningRequestContext)
                    : null;

            var streamType = frame.StreamType;
            var streamContext = new StreamContext(streamId, streamType, owningRequestContext);

            incomingStream = new IncomingStream(this.Session, streamContext);

            streamEntry = new StreamEntry(
                streamContext,
                incomingStream
            );

            this.AddStreamEntry(streamEntry);

            // Semantic notification
            this.Session.OnStreamOpened(
                incomingStream,
                new StreamMetadata(frame.Payload));

            return;
        }

        if (streamEntry is null)
        {
            throw ProtocolException.InvalidFrameSequence(frame, "Unknown StreamId");
        }
        if (!streamEntry.IsIncoming)
        {
            throw ProtocolException.ProtocolViolation(frame, "Inbound stream frames may only target streams opened by the peer.");
        }

        incomingStream = streamEntry.GetIncomingStreamOrThrow();
        switch (frame.Kind)
        {
            case ProtocolFrameKind.StreamData:
                streamEntry.Context.EnsureOpen();
                this.Session.OnStreamDataReceived(incomingStream, frame.Payload);
                break;

            case ProtocolFrameKind.StreamClose:
                streamEntry.Context.Close();
                // Mark IncomingStream as closed by peer
                incomingStream.Close();
                this.Session.OnStreamClosed(incomingStream, new StreamMetadata(frame.Payload));
                this.RemoveStream(streamId);
                break;

            default:
                throw ProtocolException.InvalidFrameSequence(frame, "Invalid stream frame kind");
        }
    }
}
