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
        // ordering: validate -> execute
        // (SendData is an operation, not a state transition)

        // validate the current state
        ArgumentNullException.ThrowIfNull(context);
        context.EnsureCanSend();

        // execute the external protocol work
        // "Outgoing" = message direction, not stream origin (see above)
        this.StreamManager.ConsumeOutgoingStreamData(
            context.StreamId, payload);
    }

    // ------------------------------------------------------------------
    // Close (graceful)
    // ------------------------------------------------------------------

    /// <summary>
    /// Cleanly closes this stream and notifies the peer.
    /// </summary>
    internal void Close(
        StreamContext streamContext,
        ReadOnlyMemory<byte> metadata)
    {
        // ordering: validate -> transition -> execute

        // validate the current state
        ArgumentNullException.ThrowIfNull(streamContext);

        // transition to the next state
        streamContext.CloseLocal();

        // execute the external protocol work
        // "Outgoing" = message direction, not stream origin (see above)
        this.StreamManager.ConsumeOutgoingStreamClose(
            streamContext.StreamId, metadata);
    }

    // ------------------------------------------------------------------
    // Abort (immediate)
    // ------------------------------------------------------------------

    /// <summary>
    /// Aborts the stream immediately and notifies the remote peer.
    /// </summary>
    internal void Abort(StreamContext streamContext, ReadOnlyMemory<byte> metadata)
    {
        // ordering: validate -> transition -> execute

        // validate the current state
        ArgumentNullException.ThrowIfNull(streamContext);

        // transition to the next state
        streamContext.Abort();

        // execute the external protocol work
        // "Outgoing" = message direction, not stream origin (see above)
        this.StreamManager.ConsumeOutgoingStreamAbort(
            streamContext.StreamId, metadata);
    }
}
