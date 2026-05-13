using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests.Api;

namespace MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

internal sealed class RequestContext
{
    internal RequestContext(
        uint requestId,
        uint? requestType,
        ProtocolDirection direction)
    {
        this.RequestId = requestId;
        this.RequestType = requestType;
        this.Direction = direction;
        this.ResponseTcs = (direction == ProtocolDirection.Outgoing)
        ? new TaskCompletionSource<IncomingResponse>(TaskCreationOptions.RunContinuationsAsynchronously)
        : null;
    }

    // ------------------------------------------------------------------
    // Identity
    // ------------------------------------------------------------------

    internal uint RequestId
    {
        get;
    }

    internal uint? RequestType
    {
        get;
    }

    internal ProtocolDirection Direction
    {
        get;
    }

    // ------------------------------------------------------------------
    // Lifecycle / state
    // ------------------------------------------------------------------

    private RequestStateMachine StateMachine
    {
        get;
    } = new RequestStateMachine();

    /// <summary>
    /// Indicates whether a Request-scoped Stream has been opened.
    /// </summary>
    internal bool HasStream
        => this.StateMachine.HasStream;

    /// <summary>
    /// Indicates whether the Request has already been responded to.
    /// </summary>
    internal bool IsCompleted
        => this.StateMachine.IsResponded;

    /// <summary>
    /// Ensures the Request is still open and able to perform Request-scoped operations.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the Request has already been responded to.
    /// </exception>
    internal void EnsureOpen()
    {
        this.StateMachine.EnsureOpen();
    }

    /// <summary>
    /// Opens the single Request-scoped Stream associated with this Request.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the Request has already been responded to, or if a
    /// Request-scoped Stream has already been opened.
    /// </exception>
    internal void OpenStream()
    {
        this.StateMachine.OpenStream();
    }

    /// <summary>
    /// Marks the request as responded (terminal).
    /// </summary>
    internal void Close()
    {
        this.StateMachine.Respond();
    }

    // ------------------------------------------------------------------
    // Response completion (outgoing requests only)
    // ------------------------------------------------------------------

    private TaskCompletionSource<IncomingResponse>? ResponseTcs
    {
        get;
    }

    private TaskCompletionSource<IncomingResponse> GetResponseTcs()
    {
        if (this.Direction != ProtocolDirection.Outgoing)
        {
            throw new InvalidOperationException(
                "Internal error: incoming requests cannot be completed with an inbound response.");
        }

        // assert that we have a task completion before we update the state machine
        var tcs = this.ResponseTcs
            ?? throw new InvalidOperationException(
                "Internal error: an outgoing request is missing its task completion source");
        
        return tcs;
    }

    internal Task<IncomingResponse> ResponseTask
        => this.GetResponseTcs().Task;

    /// <summary>
    /// Completes an outgoing Request based on an inbound terminal Response or Error frame.
    /// </summary>
    internal void CompleteWithResponse(IncomingResponse response)
    {
        // make sure we have a task completion before we update the state machine
        var tcs = this.GetResponseTcs();

        this.StateMachine.Respond();

        tcs.SetResult(response);
    }
}