using MWB.Networking.Layer2_Protocol.Frames;

namespace MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

internal sealed class StreamActions
{
    internal StreamActions(StreamManager streamManager)
    {
        this.StreamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
    }

    private StreamManager StreamManager
    {
        get;
    }

    // ------------------------------------------------------------------
    // Send Data
    // ------------------------------------------------------------------

    /// <summary>
    /// Sends data on this stream.
    /// </summary>
    internal void SendData(
        StreamContext context,
        ReadOnlyMemory<byte> payload)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.EnsureCanSend();

        this.StreamManager.Outbound.ConsumeOutgoingStreamData(
            context.StreamId,
            payload);
    }

    // ------------------------------------------------------------------
    // Close (graceful)
    // ------------------------------------------------------------------

    /// <summary>
    /// Cleanly closes this stream and notifies the peer.
    /// </summary>
    internal void Close(StreamContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        this.StreamManager.Outbound.ConsumeOutgoingStreamClose(
            context.StreamId);
    }

    // ------------------------------------------------------------------
    // Close (graceful)
    // ------------------------------------------------------------------

    /// <summary>
    /// Aborts the stream immediately and notifies the remote peer.
    /// </summary>
    internal void Abort(StreamContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        this.StreamManager.ConsumeOutgoingStreamAbort(
            context.StreamId);
    }
}
