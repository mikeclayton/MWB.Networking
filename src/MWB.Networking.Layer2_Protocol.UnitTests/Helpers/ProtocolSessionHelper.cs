using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer0_Transport.NullConnection;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer1_Framing.Encoding;
using MWB.Networking.Layer1_Framing.Encoding.NullEncoder;
using MWB.Networking.Layer2_Protocol.Driver;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;

namespace MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

internal static class ProtocolSessionHelper
{
    public static ProtocolSessionHandle CreateNullSession(
        ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;

        return ProtocolSessions.CreateSession(
            logger,
            OddEvenStreamIdParity.Even,
            runtime =>
            {
                // ----------------------------
                // Null / inert network pipeline
                // ----------------------------

                var connection = new NullNetworkConnection();
                var frameReader = new NetworkFrameReader();

                var frameWriter =
                    new NetworkFrameWriter(
                        new FrameEncoderBridge(connection));

                var adapter =
                    new NetworkAdapter(frameWriter, frameReader);

                return new ProtocolDriver(
                    logger,
                    connection,
                    new NullFrameDecoder(),
                    frameReader,
                    adapter,
                    runtime);
            });
    }
}
