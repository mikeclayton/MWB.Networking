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

    // Note:
    // -----
    //
    // "Outgoing" below refers to the direction of a protocol message
    // (i.e. StreamOpen, StreamData, StreamClose, StreamAbort):
    //
    //   * local peer sending to remote peer => "outgoing message"
    //   * remote peer sending to local peer => "incoming message"
    //
    // This is independent of the "Direction" property of the stream
    // (which simply indicates who initiated it):
    //
    //   * stream initiated by local peer => "outgoing stream"
    //   * stream initiated by remote peer => "incoming stream"
    //
    // Even for remotely-initiated ("incoming") streams, a locally-generated
    // message sent to the remote peer is "outgoing".

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

        // "Outgoing" = message direction, not stream origin (see above)
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

        // "Outgoing" = message direction, not stream origin (see above)
        this.StreamManager.Outbound.ConsumeOutgoingStreamClose(
            context.StreamId, metadata);
    }

    // ------------------------------------------------------------------
    // Abort (immediate)
    // ------------------------------------------------------------------

    /// <summary>
    /// Aborts the stream immediately and notifies the remote peer.
    /// </summary>
    internal void Abort(StreamContext context, ReadOnlyMemory<byte> metadata)
    {
        ArgumentNullException.ThrowIfNull(context);

        // "Outgoing" = message direction, not stream origin (see above)
        this.StreamManager.Outbound.ConsumeOutgoingStreamAbort(
            context.StreamId, metadata);
    }
}
