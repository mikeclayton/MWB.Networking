using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Hosting;
using MWB.Networking.Layer0_Transport.NullConnection;
using MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed.Hosting;
using MWB.Networking.Layer2_Protocol.Session.Api;

namespace MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

internal static class ProtocolSessionHelper
{
    public static ProtocolSessionHandle CreateNullSession(
        ILogger? logger = null)
    {
        var session =
            new ProtocolSessionBuilder()
                // ----------------------------
                // Logging
                // ----------------------------
                .WithLogger(logger ??= NullLogger.Instance)
                // ----------------------------
                // Protocol semantics
                // ----------------------------
                .UseEvenStreamIds()
                // ----------------------------
                // Transport + framing
                // ----------------------------
                .ConfigurePipeline(pipeline =>
                {
                    pipeline
                        .UseLengthPrefixedCodec(logger)
                        .UseConnection(() => new NullNetworkConnection());
                })
                // ----------------------------
                // Build
                // ----------------------------
                .Build();

        return session;
    }
}
