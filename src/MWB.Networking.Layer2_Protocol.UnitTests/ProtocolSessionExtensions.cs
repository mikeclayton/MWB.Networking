using MWB.Networking.Layer2_Protocol.Session;

namespace MWB.Networking.Layer2_Protocol.UnitTests;

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

    public static async Task<ProtocolFrame> AwaitOutboundAsync(
          this IProtocolSessionRuntime sessionRuntime,
          TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (sessionRuntime.TryDequeueOutboundFrame(out var frame))
            {
                return frame;
            }
            await Task.Delay(10);
        }
        throw new TimeoutException("No outbound frame arrived within timeout.");
    }
}
