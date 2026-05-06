using MWB.Networking.Layer2_Protocol.Session.Frames;

namespace MWB.Networking.Layer2_Protocol.Session.Api;

internal interface IProtocolSessionProcessor
{
    /// <summary>
    /// Drives the protocol state machine with an inbound frame.
    /// This must be synchronous and deterministic.
    /// </summary>
    void ProcessFrame(ProtocolFrame frame);
}
