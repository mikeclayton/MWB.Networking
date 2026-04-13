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
        // Validate Request-scoped frames
        if (frame.RequestId is not null)
        {
            if (!this.RequestContexts.TryGetValue(frame.RequestId.Value, out var requestContext))
            {
                throw ProtocolError(frame, "Unknown or completed RequestId");
            }

            // Ensure the Request is still open
            if (!ProtocolSession.IsTerminalRequestFrame(frame))
            {
                requestContext.EnsureOpen();
            }
        }

        // Validate Stream-scoped frames
        if (frame.StreamId is not null)
        {
            if (!this.StreamEntries.TryGetValue(frame.StreamId.Value, out var streamEntry))
            {
                throw ProtocolError(frame, "Unknown StreamId");
            }

            if (streamEntry.Context.IsRequestScoped)
            {
                var requestContext = streamEntry.Context.OwningRequest;

                // Request-scoped Streams must obey Request lifecycle rules
                requestContext.EnsureOpen();
            }

            // Session-scoped Streams require no Request validation
        }

        // If all validation succeeds, the frame is legal to send
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
