using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Session.Streams.Infrastructure;

namespace MWB.Networking.PerformanceTests.Helpers;

internal static class ProtocolSessionHelper
{
    public static ProtocolSessionHandle CreateProtocolSession(
        ILogger logger,
        OddEvenStreamIdParity parity)
    {
        var config = new ProtocolSessionOptions(
            new OddEvenStreamIdProvider(parity));

        return new ProtocolSession(logger, config).AsHandle();
    }

    public static ProtocolSessionHandle CreateOddProtocolSession(ILogger logger)
        => CreateProtocolSession(logger, OddEvenStreamIdParity.Odd);

    public static ProtocolSessionHandle CreateEvenProtocolSession(ILogger logger)
        => CreateProtocolSession(logger, OddEvenStreamIdParity.Even);
}
