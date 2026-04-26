using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Driver;
using MWB.Networking.Layer2_Protocol.Events;
using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Requests;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Streams;
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
        ProtocolSessionConfig config)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.EventManager = new EventManager(logger, this);
        this.RequestManager = new RequestManager(logger, this);
        this.StreamManager = new StreamManager(logger, this, config.OutboundStreamIdProvider);
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

    /// <summary>
    /// Deliberately not threadsafe - coordinate access in higher layers.
    /// </summary>
    private OutboundFrameQueue OutboundFrames
    {
        get;
    } = new();

    // ------------------------------------------------------------------
    // Protocol driver
    // ------------------------------------------------------------------

    private ProtocolDriver? _protocolDriver;

    private ProtocolDriver ProtocolDriver
        => _protocolDriver ?? throw new InvalidOperationException(
            "A ProtocolDriver has not been attached to the ProtocolSession.");

    internal void AttachProtocolDriver(ProtocolDriver protocolDriver)
    {
        ArgumentNullException.ThrowIfNull(protocolDriver);
        if (_protocolDriver is not null)
        {
            throw new InvalidOperationException(
            "A ProtocolDriver has already been attached to the ProtocolSession.");
        }
        _protocolDriver = protocolDriver;
    }

    // ------------------------------------------------------------------
    // Outbound queue coordination
    // ------------------------------------------------------------------

    public Task WaitForOutboundFrameAsync(CancellationToken ct)
    {
        return this.OutboundFrames.WaitForFrameAsync(ct);
    }

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

        this.OutboundFrames.Enqueue(frame);
    }
}
