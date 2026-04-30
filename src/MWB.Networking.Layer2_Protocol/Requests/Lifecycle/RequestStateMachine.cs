namespace MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

/// <summary>
/// Encapsulates the protocol lifecycle rules for a Request, its Response,
/// and an optional Request-scoped Stream into a state machine that can be
/// used to guard attempts at protocol level operations.
///
/// If an operation is invalid in the current state the machine will throw
/// an exception, which can be translated into a protocol error response
/// at the protocol session level.
/// </summary>
/// <remarks>
/// A Request produces exactly one Response. While a Request is open, it may
/// open at most one Request-scoped Stream to transmit additional data.
///
/// The Request-scoped Stream, if present, must be opened before the Response
/// is sent and carries data for the lifetime of the Request.
///
/// Sending the Response completes the Request and implicitly completes
/// and closes any associated Request-scoped Stream for protocol purposes.
///
/// Once the Response has been sent, the Request is considered complete and
/// no further Request-scoped operations are permitted.
///
/// Session-scoped Streams and Events are not governed by this lifecycle and
/// are not affected by Request completion.
/// </remarks>
internal sealed class RequestStateMachine
{
    private enum State
    {
        Open,
        Responded
    }

    private State RequestState
    {
        get;
        set;
    } = State.Open;

    /// <summary>
    /// Whether a Request-scoped Stream has been opened.
    /// </summary>
    public bool HasStream
    {
        get;
        private set;
    } = false;

    /// <summary>
    /// Whether the Request has already been responded to.
    /// </summary>
    public bool IsResponded
        => this.RequestState == State.Responded;

    /// <summary>
    /// Attempts to open a Request-scoped Stream.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the Request has already been responded to, or if a
    /// Request-scoped Stream already exists.
    /// </exception>
    public void OpenStream()
    {
        if (this.RequestState != State.Open)
        {
            throw new InvalidOperationException(
                "Cannot open a Request-scoped Stream after the Request has been responded to.");
        }

        if (this.HasStream)
        {
            throw new InvalidOperationException(
                "A Request may open at most one Request-scoped Stream.");
        }

        this.HasStream = true;
    }

    /// <summary>
    /// Marks the Request as responded.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the Request has already been responded to.
    /// </exception>
    public void Respond()
    {
        if (this.RequestState != State.Open)
        {
            throw new InvalidOperationException(
                "A Request may only be responded to once.");
        }

        this.RequestState = State.Responded;
    }

    /// <summary>
    /// Validates that a Request-scoped action is still permitted.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the Request has already been responded to.
    /// </exception>
    public void EnsureOpen()
    {
        if (this.RequestState != State.Open)
        {
            throw new InvalidOperationException(
                "No Request-scoped operations are permitted after the Response has been sent.");
        }
    }
}
