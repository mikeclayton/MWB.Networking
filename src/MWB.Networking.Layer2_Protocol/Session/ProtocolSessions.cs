using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Session.Infrastructure;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;

namespace MWB.Networking.Layer2_Protocol.Session;

public static class ProtocolSessions
{
    public static ProtocolSessionHandle CreateOddSession()
        => ProtocolSessionFactory.CreateSession(new(OddEvenStreamIdParity.Odd));

    public static ProtocolSessionHandle CreateEvenSession()
        => ProtocolSessionFactory.CreateSession(new(OddEvenStreamIdParity.Even));
}
