using MWB.Networking.Layer2_Protocol.Frames;
using System.Diagnostics.CodeAnalysis;

namespace MWB.Networking.Layer2_Protocol.Session.Api;

public interface IProtocolSessionRuntime
{
    /// <summary>
    /// Drives the protocol state machine with an inbound frame.
    /// This must be synchronous and deterministic.
    /// </summary>
    void ProcessFrame(ProtocolFrame frame);

    public Task WaitForOutboundFrameAsync(CancellationToken ct);

    bool TryDequeueOutboundFrame([NotNullWhen(true)] out ProtocolFrame frame);
}
