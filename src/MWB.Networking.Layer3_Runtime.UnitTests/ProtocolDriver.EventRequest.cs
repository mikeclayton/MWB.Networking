using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Hosting;
using MWB.Networking.Layer0_Transport.Pipes;
using MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer3_Runtime.UnitTests.Helpers;
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
    public async Task ProtocolDriver_Transmits_Requests_EndToEnd()
    {
        // ------------------------------------------------------------
        // Initialize logging
        // ------------------------------------------------------------

        using var loggerFactory =
            LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Trace)
                    .AddProvider(new TestContextLoggerProvider(TestContext));
            });

        var logger =
            loggerFactory.CreateLogger("ProtocolDriver");

        // ------------------------------------------------------------
        // Arrange: in-memory duplex transport
        // ------------------------------------------------------------
        var serverPipe = new Pipe();
        var clientPipe = new Pipe();

        using var serverConnection =
            new PipeNetworkConnection(
                reader: serverPipe.Reader,
                writer: clientPipe.Writer);

        using var clientConnection =
            new PipeNetworkConnection(
                reader: clientPipe.Reader,
                writer: serverPipe.Writer);

        // ------------------------------------------------------------
        // Build server session
        // ------------------------------------------------------------
        var serverSession =
            new ProtocolSessionBuilder()
                // ----------------------------
                // Logging
                // ----------------------------
                .WithLogger(logger)
                // ----------------------------
                // Protocol semantics
                // ----------------------------
                .UseOddStreamIds()
                // ----------------------------
                // Transport + framing
                // ----------------------------
                .ConfigurePipeline(pipeline =>
                {
                    pipeline
                        .AppendFrameCodec(
                            new LengthPrefixedFrameEncoder(logger),
                            new LengthPrefixedFrameDecoder(logger))
                        .UseConnection(() => serverConnection);
                })
                // ----------------------------
                // Build
                // ----------------------------
                .Build();

        // ------------------------------------------------------------
        // Build client session
        // ------------------------------------------------------------

        var clientSession =
            new ProtocolSessionBuilder()
                // ----------------------------
                // Logging
                // ----------------------------
                .WithLogger(logger)
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
                        .AppendFrameCodec(
                            new LengthPrefixedFrameEncoder(logger),
                            new LengthPrefixedFrameDecoder(logger))
                        .UseConnection(() => clientConnection);
                })
                // ----------------------------
                // Build
                // ----------------------------
                .Build();

        // ------------------------------------------------------------
        // Capture inbound request delivery on server
        // ------------------------------------------------------------
        var requestTcs =
            new TaskCompletionSource<(IncomingRequest Request, ReadOnlyMemory<byte> Payload)> (
                TaskCreationOptions.RunContinuationsAsynchronously);

        serverSession.Observer.RequestReceived += (request, payload) =>
        {
            requestTcs.TrySetResult((request, payload));
        };

        // ------------------------------------------------------------
        // Act: start sessions
        // ------------------------------------------------------------
        using var cts = new CancellationTokenSource();

        var serverRun = serverSession.Lifecycle.StartAsync(cts.Token);
        var clientRun = clientSession.Lifecycle.StartAsync(cts.Token);

        await Task.WhenAll(
            serverSession.Lifecycle.Ready,
            clientSession.Lifecycle.Ready);

        // ------------------------------------------------------------
        // Act: send request from client to server
        // ------------------------------------------------------------
        var requestPayload = new byte[] { 0xBE, 0xEF };

        _ = clientSession.Commands.SendRequest(requestPayload);

        // ------------------------------------------------------------
        // Assert: server observes the request (with timeout)
        // ------------------------------------------------------------
        var (receivedRequest, receivedPayload) =
            await requestTcs.Task
                .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        Assert.IsNotNull(receivedRequest);
        Assert.AreEqual(1u, receivedRequest.Context.RequestId);
        CollectionAssert.AreEqual(requestPayload, receivedPayload.ToArray());

        // ------------------------------------------------------------
        // Cleanup
        // ------------------------------------------------------------
        cts.Cancel();
        await Task.WhenAll(serverRun, clientRun);
    }
}
