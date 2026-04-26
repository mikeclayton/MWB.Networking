using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport;
using MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed.Hosting;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;
using MWB.Networking.Layer3_Hosting.Configuration;
using MWB.Networking.Layer3_Hosting.Runtime;

namespace KeyboardSharingConsole.Helpers;

internal static class SessionHostHelper
{
    public static SessionHost CreateSessionHost(
        ILogger logger,
        bool isProducer,
        INetworkConnectionProvider connectionProvider,
        Action<uint, ReadOnlyMemory<byte>>? eventReceived,
        Action<IncomingRequest, ReadOnlyMemory<byte>>? requestReceived)
    {
        var host =
            new SessionHostBuilder()
                .WithLogger(logger)
                .UseStreamIdParity(
                    isProducer
                        ? OddEvenStreamIdParity.Even
                        : OddEvenStreamIdParity.Odd)
                .ConfigurePipeline(
                    pipeline =>
                    {
                        pipeline
                            .UseLengthPrefixedCodec(logger)
                            .UseConnectionProvider(connectionProvider);
                    })
                .OnEventReceived(eventReceived)
                .OnRequestReceived(requestReceived)
                .Build();
        return host;
    }
}
