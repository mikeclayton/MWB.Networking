namespace MWB.Networking.Layer2_Protocol.Requests;

public sealed class IncomingRequest
{
    internal IncomingRequest(IProtocolRequestSink requestSink, uint requestId)
    {
        this.RequestSink = requestSink ?? throw new ArgumentNullException(nameof(requestSink));
        this.RequestId = requestId;
    }

    private IProtocolRequestSink RequestSink
    {
        get;
    }

    internal uint RequestId
    {
        get;
    }

    internal bool Completed
    {
        get;
        set;
    }

    public void Respond(ReadOnlyMemory<byte> payload)
    {
        this.EnsureNotCompleted();
        this.RequestSink.SendResponse(this.RequestId, payload);
        this.Complete();
    }

    public void Fail(ReadOnlyMemory<byte> errorPayload)
    {
        this.EnsureNotCompleted();
        this.RequestSink.SendError(this.RequestId, errorPayload);
        this.Complete();
    }

    public void Cancel()
    {
        this.EnsureNotCompleted();
        this.RequestSink.SendCancel(this.RequestId);
        this.Complete();
    }

    private void Complete()
    {
        this.Completed = true;
        this.RequestSink.CompleteRequest(this.RequestId);
    }

    private void EnsureNotCompleted()
    {
        if (this.Completed)
        {
            throw new InvalidOperationException("Request already completed.");
        }
    }
}
