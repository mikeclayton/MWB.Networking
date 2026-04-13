using MWB.Networking.Layer0_Transport;

namespace MWB.Networking.Layer1_Framing;

public interface INetworkFrameReader
{
    /// <summary>
    /// Reads exactly one NetworkFrame from the underlying connection.
    /// Blocks until a full frame is available or throws on failure.
    /// </summary>
    Task<NetworkFrame> ReadFrameAsync(
        INetworkConnection connection,
        CancellationToken ct);
}
