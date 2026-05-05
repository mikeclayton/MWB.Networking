using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Pipeline;
using MWB.Networking.Layer2_Protocol.Frames;

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
        IProtocolSessionProcessor processor,
        NetworkPipeline pipeline)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.Processor = processor ?? throw new ArgumentNullException(nameof(processor));
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

    private IProtocolSessionProcessor Processor
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
