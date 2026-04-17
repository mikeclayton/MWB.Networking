using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Hosting;
using MWB.Networking.Layer0_Transport.Pipes;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed;
using MWB.Networking.Layer2_Protocol.Driver;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;
using System.IO.Pipelines;

namespace MWB.Networking.Layer3_Runtime.UnitTests;

[TestClass]
public sealed class ProtocolDriverEndToEndTests
{
    public TestContext TestContext
    {
        get;
        set;
    }

    [TestMethod]
    public async Task ProtocolDriver_Transmits_ProtocolFrames_EndToEnd()
    {
        var logger = NullLogger.Instance;

        // ------------------------------------------------------------
        // Arrange: in-memory duplex transport
        // ------------------------------------------------------------
        var serverPipe = new Pipe();
        var clientPipe = new Pipe();

        var serverConnection =
            new PipeNetworkConnection(serverPipe.Reader, clientPipe.Writer);
        var clientConnection =
            new PipeNetworkConnection(clientPipe.Reader, serverPipe.Writer);

        // ------------------------------------------------------------
        // Build server session
        // ------------------------------------------------------------
        var serverSession = ProtocolSessions.CreateSession(
            logger,
            OddEvenStreamIdParity.Odd,
            runtime =>
            {
                var pipeline = new NetworkPipelineBuilder()
                    .AppendFrameCodec(
                        new LengthPrefixedFrameEncoder(),
                        new LengthPrefixedFrameDecoder())
                    .UseConnection(() => serverConnection)
                    .Build();

                var adapter = new NetworkAdapter(
                    pipeline.FrameWriter,
                    pipeline.FrameReader);

                return new ProtocolDriver(
                    logger,
                    pipeline.Connection,
                    pipeline.RootDecoder,
                    pipeline.FrameReader,
                    adapter,
                    runtime);
            });


        // ------------------------------------------------------------
        // Build client session
        // ------------------------------------------------------------
        var clientSession = ProtocolSessions.CreateSession(
            logger,
            OddEvenStreamIdParity.Even,
            runtime =>
            {
                var pipeline = new NetworkPipelineBuilder()
                    .AppendFrameCodec(
                        new LengthPrefixedFrameEncoder(),
                        new LengthPrefixedFrameDecoder())
                    .UseConnection(() => clientConnection)
                    .Build();

                var adapter = new NetworkAdapter(
                    pipeline.FrameWriter,
                    pipeline.FrameReader);

                return new ProtocolDriver(
                    logger,
                    pipeline.Connection,
                    pipeline.RootDecoder,
                    pipeline.FrameReader,
                    adapter,
                    runtime);
            });

        // ------------------------------------------------------------
        // Capture inbound delivery via observers
        // ------------------------------------------------------------
        var eventTcs =
            new TaskCompletionSource<(uint EventType, ReadOnlyMemory<byte> Payload)>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        var requestTcs =
            new TaskCompletionSource<IncomingRequest>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        serverSession.Observer.EventReceived += (evt, payload) =>
            eventTcs.TrySetResult((evt, payload));

        serverSession.Observer.RequestReceived += (req, payload) =>
            requestTcs.TrySetResult(req);

        // ------------------------------------------------------------
        // Act: start sessions (NOT drivers)
        // ------------------------------------------------------------
        using var cts = new CancellationTokenSource();

        var serverRun = serverSession.Lifecycle.StartAsync(cts.Token);
        var clientRun = clientSession.Lifecycle.StartAsync(cts.Token);

        await Task.WhenAll(serverSession.Lifecycle.Ready, clientSession.Lifecycle.Ready);

        // ------------------------------------------------------------
        // Act: send protocol data
        // ------------------------------------------------------------
        var eventPayload = new byte[] { 0xDE, 0xAD };
        var requestPayload = new byte[] { 0xBE, 0xEF };

        clientSession.Commands.SendEvent(1u, eventPayload);
        _ = clientSession.Commands.SendRequest(requestPayload);

        // ------------------------------------------------------------
        // Assert: inbound delivery observed
        // ------------------------------------------------------------
        Assert.AreSame(
            eventTcs.Task,
            await Task.WhenAny(
                eventTcs.Task,
                Task.Delay(TimeSpan.FromSeconds(5), TestContext.CancellationToken)));

        var receivedEvent = await eventTcs.Task;

        Assert.AreSame(
            requestTcs.Task,
            await Task.WhenAny(
                requestTcs.Task,
                Task.Delay(TimeSpan.FromSeconds(5), TestContext.CancellationToken)));

        var receivedRequest = await requestTcs.Task;

        Assert.AreEqual(1u, receivedEvent.EventType);
        CollectionAssert.AreEqual(
            eventPayload,
            receivedEvent.Payload.ToArray());

        Assert.IsNotNull(receivedRequest);
        Assert.AreEqual(1u, receivedRequest.Context.RequestId);

        // ------------------------------------------------------------
        // Cleanup
        // ------------------------------------------------------------
        cts.Cancel();
        await Task.WhenAll(serverRun, clientRun);
    }
}
