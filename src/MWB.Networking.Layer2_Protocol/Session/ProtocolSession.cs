using MWB.Networking.Layer2_Protocol.Events;
using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Requests;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Streams;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;

namespace MWB.Networking.Layer2_Protocol.Session;


/// <summary>
/// The central authority for a protocol connection.
/// 
/// ProtocolSession is the single choke point where:
/// - inbound frames are validated
/// - protocol invariants are enforced
/// - requests and streams are coordinated
/// - application intent is translated into protocol actions
/// </summary>
internal sealed partial class ProtocolSession
{
    internal ProtocolSession(OddEvenStreamIdProvider outboundStreamIdProvider)
    {
        this.EventManager = new EventManager(this);
        this.RequestManager = new RequestManager(this);
        this.StreamManager = new StreamManager(this, outboundStreamIdProvider);
    }

    internal IProtocolSessionCommands AsCommands()
        => this;

    internal IProtocolSessionDiagnostics AsDiagnostics()
        => this;

    internal IProtocolSessionObserver AsObserver()
        => this;

    internal IProtocolSessionRuntime AsRuntime()
        => this;

    internal EventManager EventManager
    {
        get;
    }

    internal RequestManager RequestManager
    {
        get;
    }

    internal StreamManager StreamManager
    {
        get;
    }

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

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------
    public async Task WaitForOutboundFrameAsync(CancellationToken ct)
    {
        await this.OutboundSignal
            .WaitAsync(ct)
            .ConfigureAwait(false);
    }

    internal void EnqueueOutboundFrame(ProtocolFrame frame)
    {
        // Validate Request-scoped frames
        if (frame.RequestId is not null)
        {
            if (!this.RequestManager.TryGetRequestContext(frame.RequestId.Value, out var requestContext))
            {
                throw ProtocolException.InvalidFrameSequence(frame, "Unknown or completed RequestId");
            }

            // Ensure the Request is still open
            if (!RequestManager.IsTerminalRequestFrame(frame))
            {
                requestContext.EnsureOpen();
            }
        }

        // Validate Stream-scoped frames
        if (frame.StreamId is not null)
        {
            if (!this.StreamManager.TryGetStreamEntry(frame.StreamId.Value, out var streamEntry))
            {
                throw ProtocolException.InvalidFrameSequence(frame, "Unknown StreamId");
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
}
