using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Driver;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;
using MWB.Networking.Logging;

namespace MWB.Networking.Layer2_Protocol.Session;

public static class ProtocolSessions
{
    [LogMethod]
    public static ProtocolSessionHandle CreateSession(
        ILogger logger,
        OddEvenStreamIdParity streamIdParity,
        Func<IProtocolSessionRuntime, ProtocolDriver> driverFactory)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(driverFactory);

        var streamIdProvider =
            new OddEvenStreamIdProvider(streamIdParity);

        var session = new ProtocolSession(
            logger,
            streamIdProvider);

        var sessionHandle = new ProtocolSessionHandle(session);

        var driver = driverFactory(session)
            ?? throw new InvalidOperationException(
                "driverFactory returned null");

        session.AttachDriver(driver);

        return sessionHandle;
    }
}