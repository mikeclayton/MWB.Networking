using MWB.Networking.Layer2_Protocol.Requests.Api;

namespace MWB.Networking.Layer2_Protocol.Requests.Lifecycle;

internal sealed class RequestEntry
{
    internal RequestEntry(RequestContext context, IncomingRequest incomingRequest)
        : this(
            context,
            incomingRequest ?? throw new ArgumentNullException(nameof(incomingRequest)),
            null)
    {
    }

    internal RequestEntry(RequestContext context, OutgoingRequest outgoingRequest)
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

    internal uint RequestId
    {
        get;
    }

    internal RequestContext Context
    {
        get;
    }

    internal bool IsIncoming => this.IncomingRequest is not null;

    internal bool IsOutgoing => this.OutgoingRequest is not null;

    internal IncomingRequest? IncomingRequest
    {
        get;
    }

    internal OutgoingRequest? OutgoingRequest
    {
        get;
    }

    internal IncomingRequest GetIncomingRequestOrThrow()
        => this.IncomingRequest ?? throw new InvalidOperationException(
            $"{nameof(RequestEntry)} does not represent an incoming request.");

    internal OutgoingRequest GetOutgoingRequestOrThrow()
        => this.OutgoingRequest ?? throw new InvalidOperationException(
            $"{nameof(RequestEntry)} does not represent an outgoing request.");
}
