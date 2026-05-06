using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Session.Frames;
using System.Diagnostics;

namespace MWB.Networking.Layer2_Protocol.Adapter;

/// <summary>
/// Bridges a ProtocolSession (ProtocolFrames) and the NetworkPipeline
/// (NetworkFrames).
///
/// The SessionAdapter:
/// - Performs mechanical frame conversion only
/// - Owns no threads, loops, or buffering
/// - Propagates backpressure synchronously
///
/// If downstream transport or encoding blocks, outbound protocol
/// frame emission is blocked automatically via synchronous event delivery.
/// </summary>
internal sealed class SessionAdapter : IDisposable
{
    private readonly ILogger _logger;
    private readonly IProtocolSessionInput _sessionInput;
    private readonly IProtocolSessionOutput _sessionOutput;
    private readonly INetworkFrameOutput _networkOutput;

    // ------------------------------------------------------------------
    // Construction
    // ------------------------------------------------------------------

    internal SessionAdapter(
        ILogger logger,
        IProtocolSessionFrameIO session,
        INetworkFrameIO networkOutput)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _networkOutput = networkOutput ?? throw new ArgumentNullException(nameof(networkOutput));

        ArgumentNullException.ThrowIfNull(session);
        _sessionInput = session;
        _sessionOutput = session;

        // Subscribe once; this is the "wiring", not a lifecycle
        _sessionOutput.OutboundFrameReady += this.OnSendProtocolFrame;
        networkOutput.NetworkFrameReady += this.OnReceiveNetworkFrame;
    }

    // ------------------------------------------------------------------
    // Inbound: pipeline (NetworkFrame) -> session (ProtocolFrame)
    // ------------------------------------------------------------------

    /// <summary>
    /// Delivers a decoded NetworkFrame into the protocol session.
    ///
    /// This method performs no protocol validation.
    /// ProtocolSession is the sole authority for semantic correctness.
    /// </summary>
    internal void OnReceiveNetworkFrame(NetworkFrame networkFrame)
    {
        ArgumentNullException.ThrowIfNull(networkFrame);

        _logger.LogTrace(
            "Received NetworkFrame {Kind}",
            networkFrame.Kind);

        var protocolFrame =
            FrameConverter.ToProtocolFrame(networkFrame);

#if ENABLE_PROTOCOL_FRAME_DIAGNOSTICS
        protocolFrame.Diagnostics.ReceivedTimestamp =
            Stopwatch.GetTimestamp();
#endif

        // Synchronous delivery preserves ordering
        _sessionInput.OnFrameReceived(protocolFrame);
    }

    // ------------------------------------------------------------------
    // Outbound: session (ProtocolFrame) -> pipeline (NetworkFrame)
    // ------------------------------------------------------------------

    private void OnSendProtocolFrame(ProtocolFrame protocolFrame)
    {
        ArgumentNullException.ThrowIfNull(protocolFrame);

        _logger.LogTrace(
            "Sending ProtocolFrame {Kind}",
            protocolFrame.Kind);

#if ENABLE_PROTOCOL_FRAME_DIAGNOSTICS
        protocolFrame.Diagnostics.SentTimestamp =
            Stopwatch.GetTimestamp();
#endif

        var networkFrame =
            FrameConverter.ToNetworkFrame(protocolFrame);

        // IMPORTANT:
        // Send MUST block or fail synchronously if downstream is congested.
        // This is how backpressure reaches the ProtocolSession.
        _networkOutput.Send(networkFrame);
    }

    // ------------------------------------------------------------------
    // Teardown
    // ------------------------------------------------------------------

    public void Dispose()
    {
        _sessionOutput.OutboundFrameReady -= this.OnSendProtocolFrame;
    }
}