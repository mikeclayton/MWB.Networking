namespace MWB.Networking.Layer0_Transport.Stack.Lifecycle;

/// <summary>
/// Represents the lifecycle state of a transport-level network connection.
///
/// This enum describes observable connection states only.
/// It does not encode policy or control behavior.
/// </summary>
public enum TransportConnectionState
{
    /// <summary>
    /// No active connection exists.
    /// This is the initial and terminal state.
    /// </summary>
    Disconnected = 0,

    /// <summary>
    /// Represents the lifecycle state of a transport-level network attempt is in progress.
    /// </summary>
    Connecting = 1,

    /// <summary>
    /// The connection is established and ready for I/O.
    /// </summary>
    Connected = 2,

    /// <summary>
    /// An orderly disconnect has begun but is not yet complete.
    /// </summary>
    Disconnecting = 3,

    /// <summary>
    /// The connection has failed due to an error.
    /// </summary>
    Faulted = 4
}
