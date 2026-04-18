namespace MWB.Networking.Layer0_Transport;

public interface ILogicalConnectionControl
{
    /// <summary>
    /// Attaches a physical network connection as the current
    /// backing connection for the logical connection.
    /// Replaces and disposes any previous backing connection.
    /// </summary>
    void Attach(INetworkConnection connection);
}
