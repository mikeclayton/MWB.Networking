using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Session.Frames;

namespace MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

/// <summary>
/// Captures all outbound frames produced by a <see cref="ProtocolSessionHandle"/>
/// by subscribing to <see cref="IProtocolSessionOutput.OutboundFrameReady"/>.
///
/// Use with <see langword="using"/> to ensure the subscription is released when
/// the capture is no longer needed.
/// </summary>
internal sealed class OutboundFrameCapture : IDisposable
{
    private readonly List<ProtocolFrame> _frames = [];
    private readonly IProtocolSessionOutput _output;

    public OutboundFrameCapture(ProtocolSessionHandle session)
    {
        _output = (IProtocolSessionOutput)session.Session;
        _output.OutboundFrameReady += Capture;
    }

    /// <summary>
    /// All frames captured since construction (or since the last <see cref="Drain"/>).
    /// </summary>
    public IReadOnlyList<ProtocolFrame> Frames => _frames;

    /// <summary>
    /// Returns all captured frames and clears the internal list, allowing
    /// independent assertions over multiple rounds of frame production.
    /// </summary>
    public IReadOnlyList<ProtocolFrame> Drain()
    {
        var copy = _frames.ToArray();
        _frames.Clear();
        return copy;
    }

    private void Capture(ProtocolFrame frame) => _frames.Add(frame);

    public void Dispose() => _output.OutboundFrameReady -= Capture;
}
