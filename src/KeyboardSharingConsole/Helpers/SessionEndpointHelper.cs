using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport;
using MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed.Hosting;
using MWB.Networking.Layer2_Protocol.Events.Api;
using MWB.Networking.Layer2_Protocol.Lifecycle.Infrastructure;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer3_Endpoint;
using MWB.Networking.Layer3_Endpoint.Hosting;

namespace KeyboardSharingConsole.Helpers;

internal static class SessionEndpointHelper
{
    public static SessionEndpoint CreateSessionEndpoint(
        ILogger logger,
        bool isProducer,
        INetworkConnectionProvider connectionProvider,
        Action<IncomingEvent, ReadOnlyMemory<byte>>? eventReceived,
        Action<IncomingRequest, ReadOnlyMemory<byte>>? requestReceived)
    {
        var builder =
            new SessionEndpointBuilder()
                .UseLogger(logger)
                .UseStreamIdParity(
                    isProducer
                        ? OddEvenStreamIdParity.Even
                        : OddEvenStreamIdParity.Odd
                )
                .ConfigurePipelineWith(
                    pipeline =>
                    {
                        pipeline
                            .UseLengthPrefixedCodec(logger)
                            .UseConnectionProvider(connectionProvider);
                    }
                );

        if (eventReceived is not null)
        {
            builder.OnEventReceived(eventReceived);
        }

        if (eventReceived is not null)
        {
            builder.OnRequestReceived(requestReceived);
        }

        var host = builder.Build();

        return host;
    }
}
