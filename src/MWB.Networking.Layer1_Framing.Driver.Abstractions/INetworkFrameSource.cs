using MWB.Networking.Layer1_Framing.Codec.Frames;

namespace MWB.Networking.Layer1_Framing.Driver.Abstractions;

public interface INetworkFrameSource
{
    /// <summary>
    /// Raised when a complete NetworkFrame has been received
    /// and decoded.
    ///
    /// Frames MUST be raised strictly in arrival order.
    /// Handlers are invoked synchronously on the driver’s
    /// execution context.
    /// </summary>
    event Action<NetworkFrame> FrameReceived;
}
