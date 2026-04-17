using MWB.Networking.Layer0_Transport;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer1_Framing.Encoding.Abstractions;

namespace MWB.Networking.Layer2_Protocol.Driver;

public sealed class ProtocolDriverOptions
{
    public ProtocolDriverOptions()
    {
    }

    public ProtocolDriverOptions(
        INetworkConnection? connection = null,
        IFrameDecoder? decoder = null,
        NetworkFrameReader? frameReader = null,
        NetworkAdapter? adapter = null)
    {
        this.Connection = connection;
        this.Decoder = decoder;
        this.FrameReader = frameReader;
        this.Adapter = adapter;
    }

    public INetworkConnection? Connection
    {
        get;
        set;
    }

    public IFrameDecoder? Decoder
    {
        get;
        set;
    }

    public NetworkFrameReader? FrameReader
    {
        get;
        set;
    }

    public NetworkAdapter? Adapter
    {
        get;
        set;
    }
}
