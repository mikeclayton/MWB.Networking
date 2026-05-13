namespace MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

/// <summary>
/// Encapsulates the protocol lifecycle rules for a Request and its Response,
/// into a state machine that can be used to guard attempts at protocol-level
/// operations.
///
/// If an operation is invalid in the current state the machine will throw
/// an exception, which can be translated into a protocol error response
/// at the protocol session level.
/// </summary>
/// <remarks>
/// A Request produces exactly one Response.
///
/// Once the Response has been sent, the Request is considered complete and
/// no further Request-scoped operations are permitted.
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
    /// Whether the Request has already been responded to.
    /// </summary>
    internal bool IsResponded
        => this.RequestState == State.Responded;

    /// <summary>
    /// Marks the Request as responded.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the Request has already been responded to.
    /// </exception>
    internal void Respond()
    {
        if (this.RequestState != State.Open)
        {
            throw new InvalidOperationException(
                "A Request may only be responded to once.");
        }

        this.RequestState = State.Responded;
    }
}
