using Microsoft.Extensions.Logging;
using MWB.Networking.Hosting;
using MWB.Networking.Layer0_Transport;
using MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;

namespace KeyboardSharingConsole.Helpers;

internal static class ProtocolSessionHelper
{
    public static ProtocolSessionHandle CreateSession(
        ILogger logger,
        bool isProducer,
        INetworkConnection connection,
        Action<uint, ReadOnlyMemory<byte>>? eventReceived,
        Action<IncomingRequest, ReadOnlyMemory<byte>>? requestReceived)
    {
        var session =
            new ProtocolSessionBuilder()
                .WithLogger(logger)
                .UseStreamIdParity(
                    isProducer
                        ? OddEvenStreamIdParity.Even
                        : OddEvenStreamIdParity.Odd)
                .ConfigurePipeline(
                    pipeline =>
                    {
                        pipeline
                            .AppendFrameCodec(
                                new LengthPrefixedFrameEncoder(logger),
                                new LengthPrefixedFrameDecoder(logger))
                             .UseConnection(() => connection);
                    })
                .ConfigureObservers(
                    observers =>
                    {
                        observers.EventReceived = eventReceived;
                        observers.RequestReceived = requestReceived;
                        //observers.StreamOpened = null;
                        //observers.StreamDataReceived = null;
                        //observers.StreamClosed = null;
                    }
                )
                .Build();
        return session;
    }
}
