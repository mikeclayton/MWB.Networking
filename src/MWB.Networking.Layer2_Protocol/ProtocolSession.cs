namespace MWB.Networking.Layer2_Protocol;

public sealed partial class ProtocolSession : IProtocolSession
{
    internal ProtocolSession()
    {
    }

    internal IProtocolSession AsSession()
        => this;

    internal IProtocolSessionRuntime AsRuntime()
        => this;

    internal SemaphoreSlim OutboundSignal
    {
        get;
    } = new(0);

    /// <summary>
    /// Deliberately not threadsafe - coordinate access in higher layers.
    /// </summary>
    private Queue<ProtocolFrame> OutboundFrames
    {
        get;
    } = [];


    public async Task WaitForOutboundFrameAsync(CancellationToken ct)
    {
        await this.OutboundSignal
            .WaitAsync(ct)
            .ConfigureAwait(false);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    internal void EnqueueOutboundFrame(ProtocolFrame frame)
    {
        this.OutboundFrames.Enqueue(frame);
        this.OutboundSignal.Release();
    }

    private static ProtocolException ProtocolError(
          ProtocolFrame frame,
          string message)
    {
        return new ProtocolException(
            ProtocolErrorKind.InvalidFrameSequence,
            $"{message} (FrameKind={frame.Kind}, RequestId={frame.RequestId}, StreamId={frame.StreamId})");
    }
}
