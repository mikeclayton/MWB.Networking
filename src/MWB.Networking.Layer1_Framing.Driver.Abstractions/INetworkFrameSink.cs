using MWB.Networking.Layer1_Framing.Codec.Frames;

namespace MWB.Networking.Layer1_Framing.Driver.Abstractions;

public interface INetworkFrameSink
{

    /// <summary>
    /// Sends a NetworkFrame downstream.
    ///
    /// This call MUST block or throw if the transport
    /// cannot currently accept more data.
    /// </summary>
    void Send(NetworkFrame frame);
}
