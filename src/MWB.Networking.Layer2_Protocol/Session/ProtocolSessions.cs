using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Session.Infrastructure;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;
using MWB.Networking.Logging;

namespace MWB.Networking.Layer2_Protocol.Session;

public static class ProtocolSessions
{
    [LogMethod]
    public static ProtocolSessionHandle CreateOddSession()
        => ProtocolSessions.CreateOddSession(
            NullLogger.Instance);

    [LogMethod]
    public static ProtocolSessionHandle CreateOddSession(ILogger logger)
        => ProtocolSessionFactory.CreateSession(
            logger, new(OddEvenStreamIdParity.Odd));

    [LogMethod]
    public static ProtocolSessionHandle CreateEvenSession()
        => ProtocolSessions.CreateOddSession(
            NullLogger.Instance);

    [LogMethod]
    public static ProtocolSessionHandle CreateEvenSession(ILogger logger)
        => ProtocolSessionFactory.CreateSession(
            logger, new(OddEvenStreamIdParity.Even));
}
