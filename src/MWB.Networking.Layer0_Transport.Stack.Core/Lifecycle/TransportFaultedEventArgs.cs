namespace MWB.Networking.Layer0_Transport.Stack.Core.Lifecycle;

public sealed class TransportFaultedEventArgs : EventArgs
{
    public TransportFaultedEventArgs(
        string message,
        Exception? exception = null,
        DateTimeOffset? occurredAt = null)
    {
        this.Message = message ?? throw new ArgumentNullException(nameof(message));
        this.Exception = exception;
        this.OccurredAt = occurredAt ?? DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// A human-readable description of the fault.
    /// This should describe what was observed, not what action should be taken.
    /// </summary>
    public string Message
    {
        get;
    }

    /// <summary>
    /// The underlying exception that caused the fault, if one exists.
    /// May be null if the fault did not originate from an exception.
    /// </summary>
    public Exception? Exception
    {
        get;
    }

    /// <summary>
    /// The time at which the fault was observed.
    /// </summary>
    public DateTimeOffset OccurredAt
    {
        get;
    }

    public override string ToString()
    {
        return Exception is null
            ? $"Transport faulted at {OccurredAt:u}: {Message}"
            : $"Transport faulted at {OccurredAt:u}: {Message} ({Exception})";
    }
}
