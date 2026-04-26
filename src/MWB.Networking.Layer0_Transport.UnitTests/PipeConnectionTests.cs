using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer0_Transport.Pipes;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed.Hosting;
using MWB.Networking.Layer1_Framing.Hosting;
using MWB.Networking.Layer1_Framing.Hosting.Manual;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer3_Hosting.Configuration;
using MWB.Networking.Logging;
using MWB.Networking.Logging.Loggers;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;

namespace MWB.Networking.Layer0_Transport.UnitTests;

public class PipeConnectionTests
{
    [TestClass]
    public sealed class SmokeTests
    {
        public TestContext TestContext
        {
            get;
            set;
        }

        [TestMethod]
        public async Task BasicRequestPayloadRoundtrip()
        {
            var (logger, loggerFactory) = DebugLoggerFactory.Create();

            // simulates a duplex connection
            var serverPipe = new Pipe();
            var clientPipe = new Pipe();

            // ------------------------------------------------------------
            // Build server session
            // ------------------------------------------------------------
            using var serverConnection = new PipeNetworkConnection(
                reader: serverPipe.Reader,
                writer: clientPipe.Writer);

            var serverSession =
                new SessionHostBuilder()
                    .WithLogger(logger)
                    .UseOddStreamIds()
                    .ConfigurePipeline(
                        pipeline =>
                        {
                            pipeline
                                .UseLengthPrefixedCodec(logger)
                                .WrapConnectionAsProvider(logger, serverConnection);
                        }
                    )
                    .Build();

            // ------------------------------------------------------------
            // Build client session
            // ------------------------------------------------------------
            var clientConnection = new PipeNetworkConnection(
                reader: clientPipe.Reader,
                writer: serverPipe.Writer);

            var clientSession =
                new SessionHostBuilder()
                    .WithLogger(logger)
                    .UseEvenStreamIds()
                    .ConfigurePipeline(pipeline =>
                    {
                        pipeline
                            .UseLengthPrefixedCodec(logger)
                            .WrapConnectionAsProvider(logger, clientConnection);
                    })
                    .Build();

            // ------------------------------------------------------------
            // Observe inbound request on server
            // ------------------------------------------------------------
            var requestTcs =
                new TaskCompletionSource<(IncomingRequest Request, ReadOnlyMemory<byte> Payload)>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

            serverSession.Observers.RequestReceived += (request, payload) =>
            {
                requestTcs.TrySetResult((request, payload));
            };

            // ------------------------------------------------------------
            // Act: start sessions
            // ------------------------------------------------------------
            using var cts = new CancellationTokenSource();

            var serverRun = serverSession.StartAsync(cts.Token);
            var clientRun = clientSession.StartAsync(cts.Token);

            await Task.WhenAll(
                serverSession.WhenReady,
                clientSession.WhenReady);

            // ------------------------------------------------------------
            // Act: send a frame from client to server
            // ------------------------------------------------------------
            var payload = new byte[] { 0x01, 0x02, 0x03 };

            clientSession.Commands.SendRequest(
                payload);

            // ------------------------------------------------------------
            // Assert: server receives the request
            // ------------------------------------------------------------
            var (receivedRequest, receivedPayload) =
                await requestTcs.Task.WaitAsync(TestContext.CancellationToken);

            // verify the message
            CollectionAssert.AreEqual(payload, receivedPayload.ToArray());

            // ------------------------------------------------------------
            // Cleanup
            // ------------------------------------------------------------
            cts.Cancel();
            await Task.WhenAll(serverRun, clientRun);
        }

        [TestMethod]
        public async Task Layer1_Framing_PipePerfTest_NonBlocking()
        {
            const int FrameCount = 100_000;

            var logger = NullLogger.Instance;
            //var (logger, loggerFactory) = DebugLoggerFactory.CreateLogger();

            using var loggerScope = logger.BeginMethodLoggingScope(this);

            // create duplex in-memory transport (pipes cross-wired)
            var clientToServer = new Pipe(new PipeOptions(
                pauseWriterThreshold: 1024 * 1024 * 50,
                resumeWriterThreshold: 1024 * 1024 * 50));
            var serverToClient = new Pipe();

            var clientConnection = new PipeNetworkConnection(
                reader: serverToClient.Reader,
                writer: clientToServer.Writer);

            var serverConnection = new PipeNetworkConnection(
                reader: clientToServer.Reader,
                writer: serverToClient.Writer);

            // ----------------------------
            // Build client pipeline
            // ----------------------------
            var clientPipeline = await new NetworkPipelineFactory()
                .UseLengthPrefixedCodec(logger)
                .WrapConnectionAsProvider(logger, clientConnection)
                .CreatePipelineAsync(TestContext.CancellationToken);
            var clientAdapter = new NetworkAdapter(
                logger,
                clientPipeline.FrameWriter,
                clientPipeline.FrameReader);

            // ----------------------------
            // Build server pipeline
            // ----------------------------
            var serverPipeline = await new NetworkPipelineFactory()
                .UseLengthPrefixedCodec(logger)
                .WrapConnectionAsProvider(logger, serverConnection)
                .CreatePipelineAsync(TestContext.CancellationToken);
            var serverAdapter = new NetworkAdapter(
                logger,
                serverPipeline.FrameWriter,
                serverPipeline.FrameReader);

            var payload = new ReadOnlyMemory<byte>([0x01, 0x02, 0x03]);

            var globalStopwatch = Stopwatch.StartNew();

            // Writer task
            var writerStopwatch = Stopwatch.StartNew();
            var writer = Task.Run(async () =>
            {
                globalStopwatch.Start();
                writerStopwatch.Start();
                for (int i = 0; i < FrameCount; i++)
                {
                    var frame = NetworkFrames.Request(
                        requestId: (uint)(i + 1),
                        payload: payload);
                    await clientAdapter.WriteFrameAsync(frame, TestContext.CancellationToken);
                }
                writerStopwatch.Stop();
                await clientToServer.Writer.CompleteAsync();
            }, TestContext.CancellationToken);

            // Reader task
            var readerStopwatch = Stopwatch.StartNew();
            var buffer = new byte[64 * 1024];
            while (true)
            {
                var bytesRead =
                    await serverConnection.ReadAsync(buffer, TestContext.CancellationToken);

                if (bytesRead == 0)
                {
                    break;
                }

                await serverPipeline.RootDecoder
                    .DecodeFrameAsync(
                        new ReadOnlySequence<byte>(buffer, 0, bytesRead),
                        serverPipeline.FrameReader,
                        TestContext.CancellationToken);
            }
            readerStopwatch.Stop();

            TestContext.WriteLine(
                $"Wrote {FrameCount} frames in {writerStopwatch.Elapsed.TotalMilliseconds:F2} ms " +
                $"({FrameCount / writerStopwatch.Elapsed.TotalSeconds:N0} frames/sec)");

            TestContext.WriteLine(
                $"Read {FrameCount} frames in {readerStopwatch.Elapsed.TotalMilliseconds:F2} ms " +
                $"({FrameCount / readerStopwatch.Elapsed.TotalSeconds:N0} frames/sec)");

            TestContext.WriteLine(
                $"Processed {FrameCount} frames in {globalStopwatch.Elapsed.TotalMilliseconds:F2} ms " +
                $"({FrameCount / globalStopwatch.Elapsed.TotalSeconds:N0} frames/sec)");
        }

        /// <remarks>
        /// This is identical to Layer2_Protocol_SendBeforeStart_IsDeliveredAfterStart,
        /// just wth 100_000 events as a performance test rather than 3 events for a
        /// correctness test. We could probably make the number of frames a test input
        /// and run both tests with the same code.
        /// </remarks>
        [TestMethod]
        public async Task Layer2_Protocol_PipePerfTest_NonBlocking()
        {
            const int FrameCount = 100_000;

            //var (logger, _) = DebugLoggerFactory.CreateLogger();
            var logger = NullLogger.Instance;

            // -------------------------------------------------
            // Transport (in-memory pipes)
            // -------------------------------------------------
            var clientToServer = new Pipe();
            var serverToClient = new Pipe();

            var clientConnection = new PipeNetworkConnection(
                reader: serverToClient.Reader,
                writer: clientToServer.Writer);

            var serverConnection = new PipeNetworkConnection(
                reader: clientToServer.Reader,
                writer: serverToClient.Writer);

            // -------------------------------------------------
            // Build sessions (NOT started yet)
            // -------------------------------------------------
            var clientSession = new SessionHostBuilder()
                .WithLogger(logger)
                .UseEvenStreamIds()
                .ConfigurePipeline(pipeline =>
                {
                    pipeline
                        .UseLengthPrefixedCodec(logger)
                        .WrapConnectionAsProvider(logger, clientConnection);
                })
                .Build();

            // used in observer.EventReceived
            var received = 0;
            var allReceived = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var readerStopwatch = (Stopwatch?)null;

            var serverSession = new SessionHostBuilder()
                .WithLogger(logger)
                .UseOddStreamIds()
                .ConfigurePipeline(pipeline =>
                {
                    pipeline
                        .UseLengthPrefixedCodec(logger)
                        .WrapConnectionAsProvider(logger, serverConnection);
                })
                .OnEventReceived((_, _) =>
                    {
                        readerStopwatch ??= Stopwatch.StartNew();
                        if (Interlocked.Increment(ref received) == FrameCount)
                        {
                            allReceived.TrySetResult();
                        }
                    }
                )
                .Build();

            var payload = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 });

            // =================================================
            // PHASE 1: ENQUEUE (non‑blocking)
            // =================================================
            var globalStopwatch = new Stopwatch();
            var writerStopwatch = new Stopwatch();
            for (var i = 0; i < FrameCount; i++)
            {
                clientSession.Commands.SendEvent(1, payload);
            }
            writerStopwatch.Stop();

            // -------------------------------------------------
            // PHASE 2: Start sessions
            // -------------------------------------------------

            // start the protocol loops
            // (wait within a maximum timeout so the test fails rather than hangs forever)
            using var lifecycleCts = new CancellationTokenSource();
            var serverRun = serverSession.StartAsync(lifecycleCts.Token);
            var clientRun = clientSession.StartAsync(lifecycleCts.Token);
            await Task
                .WhenAll(
                    serverSession.WhenReady,
                    clientSession.WhenReady)
                .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);

            // -------------------------------------------------
            // Assert: pre-start messages are delivered
            // -------------------------------------------------
            // wait for messages to be dequeued
            // (wait within a maximum timeout so the test fails rather than hangs forever)
            await allReceived.Task
                .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);
            readerStopwatch?.Stop();
            globalStopwatch.Stop();

            Assert.AreEqual(FrameCount, received);

            // -------------------------------------------------
            // Clean shutdown
            // -------------------------------------------------

            // shut down cleanly
            // (wait within a maximum timeout so the test fails rather than hangs forever)
            lifecycleCts.Cancel();
            await Task
                .WhenAll(serverRun, clientRun)
                .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);

            TestContext.WriteLine(
                $"Wrote {FrameCount} frames in {writerStopwatch.Elapsed.TotalMilliseconds:F2} ms " +
                $"({FrameCount / writerStopwatch.Elapsed.TotalSeconds:N0} frames/sec)");

            TestContext.WriteLine(
                $"Read {FrameCount} frames in {readerStopwatch?.Elapsed.TotalMilliseconds:F2} ms " +
                $"({FrameCount / readerStopwatch?.Elapsed.TotalSeconds:N0} frames/sec)");

            TestContext.WriteLine(
                $"Processed {FrameCount} frames in {globalStopwatch.Elapsed.TotalMilliseconds:F2} ms " +
                $"({FrameCount / globalStopwatch.Elapsed.TotalSeconds:N0} frames/sec)");
        }
    }
}
