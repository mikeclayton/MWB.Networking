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
            context.StreamId, payload);
    }

    // ------------------------------------------------------------------
    // Close (graceful)
    // ------------------------------------------------------------------

    /// <summary>
    /// Cleanly closes this stream and notifies the peer.
    /// </summary>
    internal void Close(
        StreamContext context,
        ReadOnlyMemory<byte> metadata)
    {
        ArgumentNullException.ThrowIfNull(context);

        this.StreamManager.Outbound.ConsumeOutgoingStreamClose(
            context.StreamId, metadata);
    }

    // ------------------------------------------------------------------
    // Abort (immediate)
    // ------------------------------------------------------------------

    /// <summary>
    /// Aborts the stream immediately and notifies the remote peer.
    /// </summary>
    internal void AbortOutgoing(StreamContext context, ReadOnlyMemory<byte> metadata)
    {
        ArgumentNullException.ThrowIfNull(context);

        this.StreamManager.Outbound.ConsumeOutgoingStreamAbort(context.StreamId, metadata);
    }

    /// <summary>
    /// Aborts the stream immediately and notifies the remote peer.
    /// </summary>
    internal void AbortIncoming(StreamContext context, ReadOnlyMemory<byte> metadata)
    {
        ArgumentNullException.ThrowIfNull(context);

        this.StreamManager.Inbound.ConsumeIncomingStreamLocalAbort(context.StreamId, metadata);
    }
}
