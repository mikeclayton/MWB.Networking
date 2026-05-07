using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer1_Framing.Driver.Abstractions;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Session.Frames;
using System.Diagnostics;

namespace MWB.Networking.Layer2_Protocol.Adapter;

/// <summary>
/// Bridges a ProtocolSession (ProtocolFrames) and the
/// network framing layer (NetworkFrames).
///
/// The SessionAdapter:
/// - Performs mechanical frame conversion only
/// - Owns no threads, loops, or buffering
/// - Propagates backpressure synchronously
///
/// If downstream transport or encoding blocks, outbound
/// protocol frame emission is blocked automatically via
/// synchronous event delivery.
/// </summary>
public sealed class SessionAdapter : IDisposable
{
    private readonly ILogger _logger;
    private readonly IProtocolSessionInput _sessionInput;
    private readonly IProtocolSessionOutput _sessionOutput;
    private readonly INetworkFrameSink _networkSink;
    private readonly INetworkFrameSource _networkSource;

    public SessionAdapter(
        ILogger logger,
        IProtocolSessionFrameIO session,
        INetworkFrameIO network)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ArgumentNullException.ThrowIfNull(session);
        _sessionInput = session;
        _sessionOutput = session;

        ArgumentNullException.ThrowIfNull(network);
        _networkSink = network;
        _networkSource = network;

        // Semantic wiring only (no lifecycle or execution ownership)
        _sessionOutput.OutboundFrameReady += this.OnSendProtocolFrame;
        _networkSource.FrameReceived += this.OnReceiveNetworkFrame;
    }

    // ------------------------------------------------------------------
    // Inbound: NetworkFrame -> ProtocolFrame
    // ------------------------------------------------------------------

    private void OnReceiveNetworkFrame(NetworkFrame networkFrame)
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
    // Outbound: ProtocolFrame -> NetworkFrame
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

        // Backpressure propagates synchronously here
        _networkSink.Send(networkFrame);
    }

    // ------------------------------------------------------------------
    // Teardown
    // ------------------------------------------------------------------

    public void Dispose()
    {
        _sessionOutput.OutboundFrameReady -= this.OnSendProtocolFrame;
        _networkSource.FrameReceived -= this.OnReceiveNetworkFrame;
    }
}