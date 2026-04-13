using System.ComponentModel;

namespace MWB.Networking.Layer2_Protocol.Internal;

/// <summary>
/// Infrastructure-only factory for creating protocol session instances.
/// </summary>
/// <remarks>
/// This factory exists solely to support runtime composition.
/// Application code should not create protocol sessions directly and
/// should instead use the public connection-level APIs.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ProtocolSessionFactory
{
    /// <summary>
    /// Creates a new <see cref="IProtocolSession"/> instance.
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
    [Obsolete("Infrastructure-only API. Applications should create connections via PeerConnectionFactory.", error: false)]
    public static IProtocolSession Create()
    {
        return new ProtocolSession();
    }
}
