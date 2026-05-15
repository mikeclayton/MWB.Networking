using MWB.Networking.Layer1_Framing.Codec.Frames;

namespace MWB.Networking.Layer2_Protocol.Adapter.UnitTests.Fakes;

/// <summary>
/// In-memory fake for <see cref="IProtocolSessionFrameIO"/>.
/// Records every inbound frame delivered via <see cref="OnFrameReceived"/>
/// and allows tests to fire <see cref="OutboundFrameReady"/> directly.
/// </summary>
internal sealed class FakeProtocolSession : IProtocolSessionFrameIO
{
    private readonly List<ProtocolFrame> _received = [];

    /// <summary>All frames delivered to the session via <see cref="OnFrameReceived"/>.</summary>
    public IReadOnlyList<ProtocolFrame> ReceivedFrames => _received;

    // -----------------------------------------------------------------------
    // IProtocolSessionInput
    // -----------------------------------------------------------------------

    public void OnFrameReceived(ProtocolFrame frame)
    {
        _received.Add(frame);
    }

    // -----------------------------------------------------------------------
    // IProtocolSessionOutput
    // -----------------------------------------------------------------------

    public event Action<ProtocolFrame>? OutboundFrameReady;

    /// <summary>
    /// Raises <see cref="OutboundFrameReady"/> as if the session had produced
    /// an outbound protocol frame.
    /// </summary>
    public void RaiseOutboundFrameReady(ProtocolFrame frame)
        => OutboundFrameReady?.Invoke(frame);
}

/// <summary>
/// In-memory fake for <see cref="INetworkFrameIO"/>.
/// Captures every frame passed to <see cref="Send"/> and allows tests to
/// fire <see cref="FrameReceived"/> directly.
/// </summary>
internal sealed class FakeNetworkIO : INetworkFrameIO
{
    private readonly List<NetworkFrame> _sent = [];

    /// <summary>All frames passed to <see cref="Send"/>.</summary>
    public IReadOnlyList<NetworkFrame> SentFrames => _sent;

    // -----------------------------------------------------------------------
    // INetworkFrameSink
    // -----------------------------------------------------------------------

    public void Send(NetworkFrame frame)
    {
        _sent.Add(frame);
    }

    // -----------------------------------------------------------------------
    // INetworkFrameSource
    // -----------------------------------------------------------------------

    public event Action<NetworkFrame>? FrameReceived;

    /// <summary>
    /// Raises <see cref="FrameReceived"/> as if the network had delivered an
    /// inbound frame.
    /// </summary>
    public void RaiseFrameReceived(NetworkFrame frame)
        => FrameReceived?.Invoke(frame);
}
