using MWB.Networking.Layer2_Protocol.Requests.Api;

namespace MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

internal sealed class RequestContext
{
    internal RequestContext(uint requestId, uint? requestType)
    {
        this.RequestId = requestId;
        this.RequestType = requestType;
    }

    internal uint RequestId
    {
        get;
    }

    internal uint? RequestType
    {
        get;
    }

    private RequestStateMachine StateMachine
    {
        get;
    } = new RequestStateMachine();

    private TaskCompletionSource<IncomingResponse> ResponseTcs
    {
        get;
    } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal Task<IncomingResponse> ResponseTask
        => this.ResponseTcs.Task;

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

    /// <summary>
    /// Completes the Request based on an inbound terminal Response or Error frame.
    /// </summary>
    /// <remarks>
    /// This method is used when processing Responses to Requests initiated by the
    /// local peer. It MUST NOT emit any protocol frames.
    /// </remarks>
    internal void CloseFromInbound(IncomingResponse incomingResponse)
    {
        // Transition the Request lifecycle to terminal
        this.StateMachine.Respond();
        // Complete the awaiting caller with the received response
        this.ResponseTcs.SetResult(incomingResponse);
    }

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
}