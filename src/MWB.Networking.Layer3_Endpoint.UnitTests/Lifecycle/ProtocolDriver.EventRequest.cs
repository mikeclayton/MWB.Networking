using MWB.Networking.Layer0_Transport.Pipes;
using MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed.Hosting;
using MWB.Networking.Layer1_Framing.Hosting.Manual;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer3_Endpoint.Hosting;
using MWB.Networking.Logging.Loggers;
using System.IO.Pipelines;

namespace _ProtocolDriver;

[TestClass]
public sealed class EventRequests
{
    public TestContext TestContext
    {
        get;
        set;
    }

    [TestCleanup]
    public void Cleanup()
    {
        // force any unobserved exceptions from finalizers to surface during
        // test runs rather than being silently ignored - this makes it easier
        // to determine *which* test caused the issue (and fix it!).
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    [TestMethod]
    public async Task ProtocolDriver_Transmits_Requests_EndToEnd()
    {
        // ------------------------------------------------------------
        // Initialize logging
        // ------------------------------------------------------------

        var (logger, loggerFactory) = DebugLoggerFactory.Create();

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
        var requestTcs =
            new TaskCompletionSource<(IncomingRequest Request, ReadOnlyMemory<byte> Payload)>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        var serverEndpoint =
            new SessionEndpointBuilder()
                .UseLogger(logger)
                .UseOddStreamIds()
                .ConfigurePipelineWith(
                    pipeline =>
                    {
                        pipeline
                            .UseLogger(logger)
                            .UseLengthPrefixedCodec(logger)
                            .WrapConnectionAsProvider(logger, serverConnection);
                    }
                )
                .OnRequestReceived(
                    (request, payload) =>
                    {
                        requestTcs.TrySetResult((request, payload));
                    }
                )
                .Build();

        // ------------------------------------------------------------
        // Build client session
        // ------------------------------------------------------------

        var clientEndpoint =
            new SessionEndpointBuilder()
                .UseLogger(logger)
                .UseEvenStreamIds()
                .ConfigurePipelineWith(
                    pipeline =>
                    {
                        pipeline
                            .UseLogger(logger)
                            .UseLengthPrefixedCodec(logger)
                            .WrapConnectionAsProvider(logger, clientConnection);
                    }
                )
                .Build();

        // ------------------------------------------------------------
        // Act: start sessions
        // ------------------------------------------------------------
        using var cts = new CancellationTokenSource();

        var serverRun = serverEndpoint.StartAsync(cts.Token);
        var clientRun = clientEndpoint.StartAsync(cts.Token);

        await Task
            .WhenAll(serverRun, clientRun)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);

        // ------------------------------------------------------------
        // Act: send request from client to server
        // ------------------------------------------------------------
        var requestPayload = new byte[] { 0xBE, 0xEF };

        _ = clientEndpoint.SendRequest(requestPayload);

        // ------------------------------------------------------------
        // Assert: server observes the request (with timeout)
        // ------------------------------------------------------------
        var (receivedRequest, receivedPayload) =
            await requestTcs.Task
                .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        Assert.IsNotNull(receivedRequest);
        Assert.AreEqual(1u, receivedRequest.RequestId);
        CollectionAssert.AreEqual(requestPayload, receivedPayload.ToArray());

        // ------------------------------------------------------------
        // Cleanup
        // ------------------------------------------------------------
        cts.Cancel();
        await Task
            .WhenAll(serverRun, clientRun)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);
    }
}
