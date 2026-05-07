using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport.Stack.Abstractions;
using MWB.Networking.Layer1_Framing.Codecs.Default.Network;
using MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.Transport;
using MWB.Networking.Layer2_Protocol.Session.Events.Api;
using MWB.Networking.Layer2_Protocol.Session.Requests.Api;
using MWB.Networking.Layer2_Protocol.Session.Streams.Infrastructure;
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
                .UseConnectionProvider(connectionProvider)
                .ConfigurePipelineWith(pipeline =>
                    pipeline
                        .UseDefaultNetworkCodec()
                        .UseLengthPrefixedTransport(logger));

        if (eventReceived is not null)
        {
            builder.OnEventReceived(eventReceived);
        }

        if (requestReceived is not null)
        {
            builder.OnRequestReceived(requestReceived);
        }

        return builder.Build();
    }
}
