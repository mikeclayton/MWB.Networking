using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Session.Api;

namespace MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

internal static class ProtocolSessionExtensions
{
    /// <summary>
    /// Drains all currently queued outbound ProtocolFrames from the session.
    /// Intended for unit tests and diagnostics only.
    /// </summary>
    public static List<ProtocolFrame> DrainOutboundFrames(this IProtocolSessionRuntime sessionRuntime)
    {
        var frames = new List<ProtocolFrame>();
        while (sessionRuntime.TryDequeueOutboundFrame(out var frame))
        {
            frames.Add(frame);
        }
        return frames;
    }
}
