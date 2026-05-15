using MWB.Networking.Layer2_Protocol.Internal;

namespace MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

/// <summary>
/// Holds the identity and lifecycle state of a protocol stream,
/// from opening through closure or abort.
/// </summary>
internal sealed partial class StreamContext
{  
    internal StreamState StreamState
    {
        get;
        private set;
    } = StreamState.None;

    // ------------------------------------------------------------------
    // Stream send/receive semantics
    // ------------------------------------------------------------------
    //
    // Streams are bidirectional and support half-close. Each peer independently
    // controls its sending direction:
    //
    //   * LocalClosed  => this peer has closed its send direction
    //                     (no further SendData allowed)
    //   * RemoteClosed => the remote peer has closed its send direction
    //                     (no further data will be received)
    //
    //
    // "Local" and "Remote" are relative to this endpoint and are independent of
    // the stream's Direction (i.e. who initiated the stream).
    //
    // Closing a stream affects only one direction:
    //
    //   * Close()       => sets LocalClosed   (this peer stops sending)
    //   * CloseRemote() => sets RemoteClosed  (peer stops sending)
    //
    // A stream is fully closed only when both sides have closed.

    internal bool IsFullyClosed
        => this.IsLocalClosed && this.IsRemoteClosed && !this.IsAborted;

    // ------------------------------------------------------------------
    // Send
    // ------------------------------------------------------------------

    //private bool CanSend
    //    => !this.IsLocalClosed && !this.IsAborted;

    /// <summary>
    /// Sending is only allowed while this peer's send direction is open.
    /// </summary>
    /// <exception cref="ProtocolException"></exception>
    internal void EnsureCanSend()
    {
        if (this.IsAborted)
        {
            throw ProtocolException.StreamAborted(
                $"Cannot send on stream {this.StreamId} - stream is aborted.");
        }
        if (this.IsLocalClosed)
        {
            throw ProtocolException.InvalidSequence(
                 $"Cannot send on stream {this.StreamId}; local send direction is closed.");
        }
    }

    // ------------------------------------------------------------------
    // Receive
    // ------------------------------------------------------------------

    //private bool CanReceive
    //    => !this.IsRemoteClosed && !this.IsAborted;

    /// <summary>
    /// Receiving is only allowed while the remote peer's send direction is open.
    /// </summary>
    /// <exception cref="ProtocolException"></exception>
    internal void EnsureCanReceive()
    {
        if (this.IsAborted)
        {
            throw ProtocolException.StreamAborted(
                $"Cannot receive on stream {this.StreamId} - stream is aborted.");
        }
        if (this.IsRemoteClosed)
        {
            // THIS is the real invariant:
            // it's an error for the remote to send data after closing its half of the stream.
            throw ProtocolException.InvalidSequence(
                $"Cannot receive on stream {this.StreamId} - remote send direction is closed.");
        }
    }

    // ------------------------------------------------------------------
    // Close
    // ------------------------------------------------------------------

    internal bool IsLocalClosed
        => this.StreamState.HasFlag(StreamState.LocalClosed);

    internal bool IsRemoteClosed
        => this.StreamState.HasFlag(StreamState.RemoteClosed);

    //private bool CanCloseLocal
    //    // close is idempotent, so allow it even if already closed
    //    => !this.IsAborted;

    //private bool CanCloseRemote
    //    // close is idempotent, so allow it even if already closed
    //    => !this.IsAborted;

    internal void EnsureCanCloseLocal()
    {
        if (this.IsAborted)
        {
            throw ProtocolException.StreamAborted(
                $"Cannot close stream {this.StreamId} - stream is aborted.");
        }
    }

    private void EnsureCanCloseRemote()
    {
        if (this.IsAborted)
        {
            throw ProtocolException.StreamAborted(
                $"Cannot close stream {this.StreamId} - stream is aborted.");
        }
    }

    /// <summary>
    /// Marks the stream as cleanly closed by the local peer.
    /// The local peer cannot send any more data, but can still receive.
    /// </summary>
    internal void CloseLocal()
    {
        this.EnsureCanCloseLocal();
        if (this.IsLocalClosed)
        {
            // already closed — idempotent
            return;
        }
        // callers check IsFullyClosed themselves
        this.StreamState |= StreamState.LocalClosed;
    }

    /// <summary>
    /// Marks the stream as cleanly closed by the remote peer.
    /// The remote peer cannot send any more data, but can still receive.
    /// </summary>
    internal void CloseRemote()
    {
        this.EnsureCanCloseLocal();
        if (this.IsRemoteClosed)
        {
            // already closed — idempotent
            return;
        }
        this.StreamState |= StreamState.RemoteClosed;
    }

    // ------------------------------------------------------------------
    // Abort
    // ------------------------------------------------------------------

    internal bool IsAborted
        => this.StreamState.HasFlag(StreamState.Aborted);

    /// <summary>
    /// Abort this stream due to a failure condition signalled by
    /// either the local or remote peer.
    /// </summary>
    internal void Abort()
    {
        // callers publish and remove themselves
        // note - *overwrite* the property value (don't just set the Aborted flag)
        // to ensure other flags like LocalClosed and RemoteClosed get cleared on abort.
        this.StreamState = StreamState.Aborted;
    }
}
