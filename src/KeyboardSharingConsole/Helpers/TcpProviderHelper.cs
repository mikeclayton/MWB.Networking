using KeyboardSharingConsole.CommandLine;
using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport.Tcp;
using System.Net;

namespace KeyboardSharingConsole.Helpers;

internal class TcpProviderHelper
{
    public void CreateTcpNetworkConnectionProvider(ILogger logger, CommandLineOptions options, ConnectionDirection preferredArbitrationDirection);
    {

        using var provider =
        new TcpNetworkConnectionProvider(
            logger,
            new TcpNetworkConnectionConfig(
                localEndpoint: new IPEndPoint(
                    IPAddress.Loopback, options.ListenPort),
                remoteEndpoint: new IPEndPoint(
                    IPAddress.Loopback, options.ConnectPort),
                noDelay: true),
            preferredArbitrationDirection: preferredArbitrationDirection
        );
    }
}
