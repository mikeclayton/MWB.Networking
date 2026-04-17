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
        ProtocolDriverOptions driverOptions)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(driverOptions);

        var streamIdProvider =
            new OddEvenStreamIdProvider(streamIdParity);

        var session = new ProtocolSession(
            logger,
            streamIdProvider,
            driverOptions);

        var sessionHandle = new ProtocolSessionHandle(session);

        return sessionHandle;
    }
}
