namespace MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

internal sealed partial class RequestContext
{
    // Encapsulates the protocol lifecycle rules for a Request and its Response,
    // into a state machine that can be used to guard attempts at protocol-level
    // operations.
    //
    // If an operation is invalid in the current state the machine will throw
    // an exception, which can be translated into a protocol error response
    // at the protocol session level.
    //
    // A Request produces exactly one Response.
    //
    // Once the Response has been sent, the request becomes terminal and no further
    // request request-scoped operations are permitted.

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

    // ------------------------------------------------------------------
    // Respond
    // ------------------------------------------------------------------

    /// <summary>
    /// Whether the Request has already been responded to.
    /// </summary>
    internal bool HasResponded
        => this.RequestState == State.Responded;

    internal bool CanRespond
        => this.RequestState == State.Open;

    internal void EnsureCanRespond()
    {
        if (!this.CanRespond)
        {
            throw new InvalidOperationException(
                "Request has already been responded to.");
        }
    }

    /// <summary>
    /// Marks the Request as responded.
    /// </summary>
    internal void Respond()
    {
        this.EnsureCanRespond();
        this.RequestState = State.Responded;
    }
}
