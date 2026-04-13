using MWB.Networking.Layer0_Transport;

namespace MWB.Networking.Layer1_Framing;

public interface INetworkFrameWriter
{
    /// <summary>
    /// Writes a complete NetworkFrame to the underlying connection.
    /// The implementation is responsible for writing header and payload
    /// in correct order and framing.
    /// </summary>
    Task WriteFrameAsync(
        INetworkConnection connection,
        NetworkFrame frame,
        CancellationToken ct);
}
