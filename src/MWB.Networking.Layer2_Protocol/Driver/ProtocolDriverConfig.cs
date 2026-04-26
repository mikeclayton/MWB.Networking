using MWB.Networking.Layer0_Transport;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer1_Framing.Encoding.Abstractions;

namespace MWB.Networking.Layer2_Protocol.Driver;

public sealed class ProtocolDriverConfig
{
    public ProtocolDriverConfig(
        IFrameDecoder? decoder = null,
        NetworkFrameReader? frameReader = null,
        NetworkAdapter? adapter = null)
    {
        this.Decoder = decoder;
        this.FrameReader = frameReader;
        this.Adapter = adapter;
    }

    public INetworkConnection? Connection
    {
        get;
    }

    public IFrameDecoder? Decoder
    {
        get;
    }

    public NetworkFrameReader? FrameReader
    {
        get;
    }

    public NetworkAdapter? Adapter
    {
        get;
    }
}
