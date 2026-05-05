using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Pipeline;
using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Session.Api;

namespace MWB.Networking.Layer2_Protocol.Adapter;

/// <summary>
/// Drivers a protocol session over a transport by running
/// read / write / consume loops.
///
/// Layer 2.5:
/// - Knows protocol internals
/// - Owns execution, concurrency, and shutdown
/// - Does NOT own lifecycle policy
/// - Does NOT define protocol semantics
/// </summary>
public sealed partial class SessionAdapter
{
    // ------------------------------------------------------------------
    // Construction
    // ------------------------------------------------------------------

    internal SessionAdapter(
        ILogger logger,
        IProtocolSessionFrameIO frameIO,
        NetworkPipeline pipeline)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.SessionInput = frameIO ?? throw new ArgumentNullException(nameof(frameIO));
        this.SessionOutput = frameIO ?? throw new ArgumentNullException(nameof(frameIO));
        this.Pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
    }

#if ENABLE_PROTOCOL_FRAME_DIAGNOSTICS
    private RingBuffer<ProtocolFrame> RecentInboundFrames
    {
        get;
    } = new(capacity: 100_000);

    private RingBuffer<ProtocolFrame> RecentOutboundFrames
    {
        get;
    } = new(capacity: 100_000);
#endif

    // ------------------------------------------------------------------
    // Dependencies
    // ------------------------------------------------------------------

    public ILogger Logger
    {
        get;
    }

    private IProtocolSessionInput SessionInput
    {
        get;
    }

    private IProtocolSessionOutput SessionOutput
    {
        get;
    }

    private NetworkPipeline Pipeline
    {
        get;
    }

    /// <summary>
    /// Serializes semantic execution against the runtime.
    /// Ensures protocol state is single-threaded even though
    /// multiple driver loops are running.
    /// </summary>
    private SemaphoreSlim ProcessorGate
    {
        get;
    } = new(1, 1);
}
