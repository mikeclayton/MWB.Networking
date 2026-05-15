namespace MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

/// <summary>
/// Represents the lifecycle state of a bidirectional stream.
///
/// This model uses a "half-close" approach, where each side of the stream
/// independently controls its ability to send data:
///
/// - <see cref="LocalClosed"/> indicates that the local side has finished sending data.
/// - <see cref="RemoteClosed"/> indicates that the remote peer has finished sending data.
/// - A stream is considered fully closed when both flags are set.
/// - <see cref="Aborted"/> indicates an immediate, terminal failure state and overrides all other states.
///
/// The absence of flags (<see cref="None"/>) represents a fully open stream,
/// where both sides may continue sending data.
///
/// This allows scenarios where one side completes its transmission while the
/// other continues (e.g. request/response streaming or end-of-stream acknowledgements).
/// </summary>
[Flags]
internal enum StreamState
{
    /// <summary>
    /// The stream is fully open; both sides may send data.
    /// </summary>
    None = 0,

    /// <summary>
    /// The local side has closed its send direction.
    /// The remote peer may still send data.
    /// </summary>
    LocalClosed = 1,

    /// <summary>
    /// The remote peer has closed its send direction.
    /// The local side may still send data.
    /// </summary>
    RemoteClosed = 2,

    /// <summary>
    /// The stream has been aborted and is no longer usable.
    /// This is a terminal state that overrides all other states.
    /// </summary>
    Aborted = 4
}
