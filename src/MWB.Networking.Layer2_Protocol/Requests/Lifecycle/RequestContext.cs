using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

/// <summary>
/// Holds the identity and lifecycle state of a protocol request,
/// from creation through completion or cancellation.
/// </summary>
internal sealed class RequestContext
{
    private RequestContext(
        uint requestId,
        uint? requestType,
        ProtocolDirection direction)
    {
        this.RequestId = requestId;
        this.RequestType = requestType;
        this.Direction = direction;

        // note - ResponseTcs is null for incoming requests because
        // only outgoing requests can be awaited for a response
        this.ResponseTcs = (direction == ProtocolDirection.Outgoing)
            ? new TaskCompletionSource<IncomingResponse>(TaskCreationOptions.RunContinuationsAsynchronously)
            : null;
    }

    internal static RequestContext CreateIncoming(
        uint requestId,
        uint? requestType,
        RequestActions actions,
        ReadOnlyMemory<byte> payload)
    {
        var context = new RequestContext(requestId, requestType, ProtocolDirection.Incoming);
        var request = new IncomingRequest(context, actions, payload);
        context.IncomingRequest = request;
        context.OutgoingRequest = null;
        return context;
    }

    internal static RequestContext CreateOutgoing(
        uint requestId,
        uint? requestType,
        RequestActions actions,
        ReadOnlyMemory<byte> payload)
    {
        var context = new RequestContext(requestId, requestType, ProtocolDirection.Outgoing);
        var request = new OutgoingRequest(context, actions, payload);
        context.IncomingRequest = null;
        context.OutgoingRequest = request;
        return context;
    }

    internal IncomingRequest? IncomingRequest
    {
        get;
        private set;
    }

    internal IncomingRequest GetIncomingRequest()
    {
        if ((this.Direction != ProtocolDirection.Incoming) || (this.IncomingRequest is null))
        {
            throw ProtocolException.ProtocolViolation(
                $"Request {this.RequestId} is not inbound.");
        }
        return this.IncomingRequest;
    }

    internal OutgoingRequest? OutgoingRequest
    {
        get;
        private set;
    }

    internal OutgoingRequest GetOutgoingRequest()
    {
        if ((this.Direction != ProtocolDirection.Outgoing) || (this.OutgoingRequest is null))
        {
            throw ProtocolException.ProtocolViolation(
                $"Request {this.RequestId} is not outbound.");
        }
        return this.OutgoingRequest;
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
    /// Indicates whether the Request has already been responded to.
    /// </summary>
    internal bool IsCompleted
        => this.StateMachine.IsResponded;

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