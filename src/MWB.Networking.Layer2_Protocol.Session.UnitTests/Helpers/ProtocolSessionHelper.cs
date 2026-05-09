using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Session.Hosting;
using MWB.Networking.Layer2_Protocol.Session.Streams.Infrastructure;

namespace MWB.Networking.Layer2_Protocol.Session.UnitTests.Helpers;

internal static class ProtocolSessionHelper
{
    public static ProtocolSessionHandle CreateProtocolSession(
        ILogger logger,
        OddEvenStreamIdParity parity)
    {
        return new ProtocolSessionBuilder()
            .UseLogger(logger)
            .UseStreamIdParity(parity)
            .Build();
    }

    public static ProtocolSessionHandle CreateOddProtocolSession(ILogger logger)
        => CreateProtocolSession(logger, OddEvenStreamIdParity.Odd);

    public static ProtocolSessionHandle CreateEvenProtocolSession(ILogger logger)
        => CreateProtocolSession(logger, OddEvenStreamIdParity.Even);
}
