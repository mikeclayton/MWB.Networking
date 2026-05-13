using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;
using MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Streams;

internal sealed class StreamManagerOutbound
{
    internal StreamManagerOutbound(
        ProtocolSession session,
        StreamManager streamManager,
        StreamContexts streamContexts,
        OddEvenStreamIdProvider streamIdProvider)
    {
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.StreamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        this.StreamContexts = streamContexts ?? throw new ArgumentNullException(nameof(streamContexts));
        this.StreamIdProvider = streamIdProvider ?? throw new ArgumentNullException(nameof(streamIdProvider));
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

    private OddEvenStreamIdProvider StreamIdProvider
    {
        get;
    }

    internal OutgoingStream OpenSessionStream(uint? streamType = null, ReadOnlyMemory<byte> metadata = default)
    {
        var streamId = this.StreamIdProvider.AllocateOutbound();

        var streamContext = new StreamContext(
            streamId: streamId,
            streamType: streamType
        );

        var stream = new OutgoingStream(this.Session, streamContext, streamId);

        this.StreamContexts.Add(streamContext);

        this.Session.SendOutboundFrame(
            ProtocolFrames.StreamOpen(streamId, streamType: streamType, metadata: metadata));

        return stream;
    }

    internal void CloseOutgoingStream(uint streamId)
    {
        if (!this.StreamContexts.TryGet(streamId, out var streamContext))
        {
            return;
        }

        streamContext.Close();

        this.Session.SendOutboundFrame(
            ProtocolFrames.StreamClose(streamId));

        this.StreamManager.RemoveStream(streamId);
    }

    internal void AbortStream(uint streamId)
    {
        if (!this.StreamContexts.TryGet(streamId, out var streamContext))
        {
            return;
        }

        streamContext.Close();

        // locally-owned stream aborted by local peer
        // so notify the remote peer to abort the stream as well
        this.Session.SendOutboundFrame(
            ProtocolFrames.StreamAbort(streamId));

        this.StreamManager.RemoveStream(streamId);
    }
}
