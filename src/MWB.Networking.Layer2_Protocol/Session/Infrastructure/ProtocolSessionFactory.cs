using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;

namespace MWB.Networking.Layer2_Protocol.Session.Infrastructure;

/// <summary>
/// Infrastructure-only factory for creating protocol session instances.
/// </summary>
/// <remarks>
/// This factory exists solely to support runtime composition.
/// Application code should not create protocol sessions directly and
/// should instead use the public connection-level APIs.
/// </remarks>
internal static class ProtocolSessionFactory
{
    /// <summary>
    /// Creates a new <see cref="IProtocolSessionObserver"/> instance.
    /// </summary>
    /// <remarks>
    /// This method is intended for use by library infrastructure and
    /// runtime components only. It is not part of the supported
    /// application-facing API surface.
    ///
    /// Applications should create connections via the higher-level
    /// connection or peer factory APIs rather than invoking this method
    /// directly.
    /// </remarks>
    public static ProtocolSessionHandle CreateSession(OddEvenStreamIdProvider outboundStreamIdProvider)
    {
        return new(new ProtocolSession(outboundStreamIdProvider));
    }
}
