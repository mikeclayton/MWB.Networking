namespace MWB.Networking.Layer0_Transport.Lifecycle.Stack;

/// <summary>
/// Provides information about a transport disconnection event.
/// </summary>
/// <remarks>
/// A transport disconnection represents the end of a physical
/// connection attempt. It may occur after a graceful shutdown,
/// a remote closure, or following a fault.
/// </remarks>
public sealed class TransportDisconnectedEventArgs : EventArgs
{
    public TransportDisconnectedEventArgs(
        string message,
        Exception? exception = null,
        DateTimeOffset? occurredAt = null)
    {
        this.Message = message ?? throw new ArgumentNullException(nameof(message));
        this.Exception = exception;
        this.OccurredAt = occurredAt ?? DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// A human-readable description of why the connection was disconnected.
    /// </summary>
    /// <remarks>
    /// This message describes what was observed, not what action
    /// should be taken in response.
    /// </remarks>
    public string Message
    {
        get;
    }

    /// <summary>
    /// The underlying exception that caused the disconnect, if one exists.
    /// May be null if the disconnect did not originate from an exception.
    /// </summary>
    public Exception? Exception
    {
        get;
    }

    /// <summary>
    /// The time at which the disconnection was observed.
    /// </summary>
    public DateTimeOffset OccurredAt
    { 
        get;
    }

    public override string ToString()
        => $"Transport disconnected at {OccurredAt:u}: {Message}";
}