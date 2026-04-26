using MWB.Networking.Layer2_Protocol.Driver;
using MWB.Networking.Layer2_Protocol.Session.Api;

namespace MWB.Networking.Layer2_Protocol.Runtime;

/// <summary>
/// Result of atomic protocol instance construction.
/// Exists only to prevent half-built state from escaping.
/// </summary>
public sealed class ProtocolInstance
{
    internal ProtocolInstance(
        ProtocolSessionHandle session,
        ProtocolDriver driver)
    {
        this.ProtocolSession = session ?? throw new ArgumentNullException(nameof(session));
        this.ProtocolDriver = driver ?? throw new ArgumentNullException(nameof(driver));
    }

    public ProtocolSessionHandle ProtocolSession
    {
        get;
    }

    public ProtocolDriver ProtocolDriver
    {
        get;
    }
}
