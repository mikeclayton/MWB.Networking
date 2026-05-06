using MWB.Networking.Layer2_Protocol.Session.Requests.Api;

namespace MWB.Networking.Layer2_Protocol.Session.Requests.Lifecycle;

internal sealed class RequestEntry
{
    public RequestEntry(RequestContext context, IncomingRequest incomingRequest)
        : this(
            context,
            incomingRequest ?? throw new ArgumentNullException(nameof(incomingRequest)),
            null)
    {
    }

    public RequestEntry(RequestContext context, OutgoingRequest outgoingRequest)
        : this(
            context,
            null,
            outgoingRequest ?? throw new ArgumentNullException(nameof(outgoingRequest)))
    {
    }

    private RequestEntry(RequestContext context, IncomingRequest? incomingRequest, OutgoingRequest? outgoingRequest)
    {
        // Enforce: exactly one Request must be provided
        if ((incomingRequest is null) == (outgoingRequest is null))
        {
            throw new ArgumentException(
                "RequestEntry must have exactly one of IncomingRequest or OutgoingRequest.");
        }

        this.RequestId = incomingRequest?.RequestId ?? outgoingRequest?.RequestId
            ?? throw new InvalidOperationException();
        this.Context = context;
        this.IncomingRequest = incomingRequest;
        this.OutgoingRequest = outgoingRequest;
    }

    public uint RequestId
    {
        get;
    }

    public RequestContext Context
    {
        get;
    }

    public bool IsIncoming => this.IncomingRequest is not null;

    public bool IsOutgoing => this.OutgoingRequest is not null;

    public IncomingRequest? IncomingRequest
    {
        get;
    }

    public OutgoingRequest? OutgoingRequest
    {
        get;
    }

    public IncomingRequest GetIncomingRequestOrThrow()
        => this.IncomingRequest ?? throw new InvalidOperationException(
            $"{nameof(RequestEntry)} does not represent an incoming request.");

    public OutgoingRequest GetOutgoingRequestOrThrow()
        => this.OutgoingRequest ?? throw new InvalidOperationException(
            $"{nameof(RequestEntry)} does not represent an outgoing request.");
}
