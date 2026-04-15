using System.Net;

namespace MWB.Networking.Layer0_Transport.Tcp;

public class TcpNetworkConnectionConfig
{
    public const int DefaultMaxFrameSize = 64 * 1024; // 64 KB

    public TcpNetworkConnectionConfig(
        IPEndPoint? localEndpoint,
        IPEndPoint? remoteEndpoint,
        int maxFrameSize = TcpNetworkConnectionConfig.DefaultMaxFrameSize,
        bool noDelay = true)
    {
        this.LocalEndpoint = localEndpoint;
        this.RemoteEndpoint = remoteEndpoint;
        this.MaxFrameSize = maxFrameSize;
        this.NoDelay = noDelay;
    }

    /// <summary>
    /// Local endpoint to bind to for inbound connections.
    /// If null, no listener will be started.
    /// </summary>
    public IPEndPoint? LocalEndpoint
    {
        get;
    }

    /// <summary>
    /// Remote endpoint to connect to for outbound connections.
    /// If null, no outbound connection will be attempted.
    /// </summary>
    public IPEndPoint? RemoteEndpoint
    {
        get;
    }

    public int MaxFrameSize
    {
        get;
    }


    /// <summary>
    /// Whether Nagle's algorithm should be disabled.
    /// </summary>
    public bool NoDelay
    {
        get;
    }
}
