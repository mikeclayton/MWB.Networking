using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Driver;
using MWB.Networking.Layer2_Protocol.Events;
using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Requests;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Streams;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;
using MWB.Networking.Logging;
using System.Diagnostics;

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
internal sealed partial class ProtocolSession : IHasLogger
{
    internal ProtocolSession(
        ILogger logger,
        OddEvenStreamIdProvider outboundStreamIdProvider,
        ProtocolDriverOptions driverOptions)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.EventManager = new EventManager(logger, this);
        this.RequestManager = new RequestManager(logger, this);
        this.StreamManager = new StreamManager(logger, this, outboundStreamIdProvider);
        this.ProtocolDriver = new ProtocolDriver(logger, this, driverOptions);
    }

    public ILogger Logger
    {
        get;
    }

    internal ProtocolSessionHandle AsHandle()
    {
        return new ProtocolSessionHandle(this);
    }

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

    internal SemaphoreSlim OutboundFrameAvailableSignal
    {
        get;
    } = new(initialCount: 0);

    /// <summary>
    /// Deliberately not threadsafe - coordinate access in higher layers.
    /// </summary>
    private Queue<ProtocolFrame> OutboundFrames
    {
        get;
    } = [];

    private Lock OutboundFramesLock
    {
        get;
    } = new();

    private ProtocolDriver ProtocolDriver
    {
        get;
    }

    // ------------------------------------------------------------------
    // Outbound queue coordination
    // ------------------------------------------------------------------

    public async Task WaitForOutboundFrameAsync(CancellationToken ct)
    {
        // check if there are any frame on the queue already
        // (e.g. sent before the write loop started)

        // Fast path: check the predicate under the same lock as enqueue/dequeue
        using (var lockScope = this.OutboundFramesLock.EnterScope())
        {
            if (this.OutboundFrames.Count > 0)
            {
                return;
            }
        }

        // Slow path: wait for a new frame to be enqueued
        await this.OutboundFrameAvailableSignal
            .WaitAsync(ct)
            .ConfigureAwait(false);
    }

    //internal bool TryDequeueOutboundFrame(out ProtocolFrame frame)
    //{
    //    using var lockScope = this.OutboundFramesLock.EnterScope();

    //    if (this.OutboundFrames.Count == 0)
    //    {
    //        frame = default!;
    //        return false;
    //    }

    //    frame = this.OutboundFrames.Dequeue();
    //    return true;
    //}

    //[LogMethod]
    internal void EnqueueOutboundFrame(ProtocolFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        // --------------------------------------------------------------
        // Validate Request-scoped frames
        // --------------------------------------------------------------
        if (frame.RequestId is not null)
        {
            if (!this.RequestManager.TryGetRequestContext(
                    frame.RequestId.Value,
                    out var requestContext))
            {
                throw ProtocolException.InvalidFrameSequence(
                    frame,
                    "Unknown or completed RequestId");
            }

            // Ensure the Request is still open
            if (!RequestManager.IsTerminalRequestFrame(frame))
            {
                requestContext.EnsureOpen();
            }
        }

        // --------------------------------------------------------------
        // Validate Stream-scoped frames
        // --------------------------------------------------------------
        if (frame.StreamId is not null)
        {
            if (!this.StreamManager.TryGetStreamEntry(
                    frame.StreamId.Value,
                    out var streamEntry))
            {
                throw ProtocolException.InvalidFrameSequence(
                    frame,
                    "Unknown StreamId");
            }

            if (streamEntry.Context.IsRequestScoped)
            {
                streamEntry.Context
                    .OwningRequest
                    .EnsureOpen();
            }
        }

        // --------------------------------------------------------------
        // Enqueue outbound frame
        // --------------------------------------------------------------
#if ENABLE_PROTOCOL_FRAME_DIAGNOSTICS
        frame.Diagnostics.EnqueuedTimestamp =
            Stopwatch.GetTimestamp();
#endif

        using (var lockScope = this.OutboundFramesLock.EnterScope())
        {
            this.OutboundFrames.Enqueue(frame);
        }

        this.OutboundFrameAvailableSignal.Release();
    }
}
