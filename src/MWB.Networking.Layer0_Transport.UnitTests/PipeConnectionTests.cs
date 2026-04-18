using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Hosting;
using MWB.Networking.Layer0_Transport.Pipes;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Logging;
using MWB.Networking.UnitTest.Helpers.Logging;
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
            var (logger, loggerFactory) = TestContextLoggerFactory.CreateLogger(this.TestContext);

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
                new ProtocolSessionBuilder()
                    .WithLogger(logger)
                    .UseOddStreamIds()
                    .ConfigurePipeline(p =>
                    {
                        p.AppendFrameCodec(
                             new LengthPrefixedFrameEncoder(logger),
                             new LengthPrefixedFrameDecoder(logger))
                         .UseConnection(() => serverConnection);
                    })
                    .Build();

            // ------------------------------------------------------------
            // Build client session
            // ------------------------------------------------------------
            var clientConnection = new PipeNetworkConnection(
                reader: clientPipe.Reader,
                writer: serverPipe.Writer);

            var clientSession =
                new ProtocolSessionBuilder()
                    .WithLogger(logger)
                    .UseEvenStreamIds()
                    .ConfigurePipeline(p =>
                    {
                        p.AppendFrameCodec(
                             new LengthPrefixedFrameEncoder(logger),
                             new LengthPrefixedFrameDecoder(logger))
                         .UseConnection(() => clientConnection);
                    })
                    .Build();

            // ------------------------------------------------------------
            // Observe inbound request on server
            // ------------------------------------------------------------
            var requestTcs =
                new TaskCompletionSource<(IncomingRequest Request, ReadOnlyMemory<byte> Payload)>(
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
        public async Task PipePerfTestBlockingWriteBlockingRead()
        {
            const int FrameCount = 100_000;

            var (logger, loggerFactory) = DebugLoggerFactory.CreateLogger();
            using var loggerScope = logger.EnterMethod(this);

            logger.LogDebug("TEST: If you see this, the logger itself works");
            logger.LogDebug(nameof(PipePerfTestBlockingWriteBlockingRead));

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
            var clientPipeline = new NetworkPipelineBuilder()
                .AppendFrameCodec(
                    encoder: new LengthPrefixedFrameEncoder(logger),
                    decoder: new LengthPrefixedFrameDecoder(logger))
                .UseConnection(() => clientConnection)
                .Build();

            var clientAdapter = new NetworkAdapter(
                logger,
                clientPipeline.FrameWriter,
                clientPipeline.FrameReader);

            // ----------------------------
            // Build server pipeline
            // ----------------------------
            var serverPipeline = new NetworkPipelineBuilder()
                .AppendFrameCodec(
                    encoder: new LengthPrefixedFrameEncoder(logger),
                    decoder: new LengthPrefixedFrameDecoder(logger))
                .UseConnection(() => serverConnection)
                .Build();

            var serverAdapter = new NetworkAdapter(
                logger,
                serverPipeline.FrameWriter,
                serverPipeline.FrameReader);

            var payload = new ReadOnlyMemory<byte>([0x01, 0x02, 0x03]);

            var globalStopwatch = new Stopwatch();

            // Writer task
            var writerStopwatch = new Stopwatch();
            var writer = Task.Run(async () =>
            {
                globalStopwatch.Start();
                writerStopwatch.Start();
                for (int i = 0; i < FrameCount; i++)
                {
                    var frame = new NetworkFrame(
                        kind: NetworkFrameKind.Request,
                        eventType: null,
                        requestId: (uint)(i + 1),
                        streamId: null,
                        payload: payload);
                    await clientAdapter.WriteFrameAsync(frame, TestContext.CancellationToken);
                }
                writerStopwatch.Stop();
                await clientToServer.Writer.CompleteAsync();
            }, TestContext.CancellationToken);
            await writer;

            // Reader task
            var readerStopwatch = new Stopwatch();
            var reader = Task.Run(async () =>
            {
                readerStopwatch.Start();
                for (int i = 0; i < FrameCount; i++)
                {
                    var frame = await serverAdapter.ReadFrameAsync(TestContext.CancellationToken);
                    // Optional correctness checks (cheap and safe)
                    Assert.AreEqual(NetworkFrameKind.Request, frame.Kind);
                    Assert.AreEqual((uint)(i + 1), frame.RequestId);
                    Assert.AreEqual(payload.Length, frame.Payload.Length);
                }
                readerStopwatch.Stop();
                globalStopwatch.Stop();
            }, TestContext.CancellationToken);
            await reader;

            await Task.WhenAll(writer, reader);

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

        [TestMethod]
        public async Task Layer1_Framing_PipePerfTest_NonBlocking()
        {
            const int FrameCount = 100_000;

            //var logger = LoggingHelper.CreateTestContextLogger(this.TestContext);
            var (logger, loggerFactory) = DebugLoggerFactory.CreateLogger();
            using var loggerScope = logger.EnterMethod(this);

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
            var clientPipeline = new NetworkPipelineBuilder()
                .AppendFrameCodec(
                    encoder: new LengthPrefixedFrameEncoder(logger),
                    decoder: new LengthPrefixedFrameDecoder(logger))
                .UseConnection(() => clientConnection)
                .Build();
            var clientAdapter = new NetworkAdapter(
                logger,
                clientPipeline.FrameWriter,
                clientPipeline.FrameReader);

            // ----------------------------
            // Build server pipeline
            // ----------------------------
            var serverPipeline = new NetworkPipelineBuilder()
                .AppendFrameCodec(
                    encoder: new LengthPrefixedFrameEncoder(logger),
                    decoder: new LengthPrefixedFrameDecoder(logger))
                .UseConnection(() => serverConnection)
                .Build();
            var serverAdapter = new NetworkAdapter(
                logger,
                serverPipeline.FrameWriter,
                serverPipeline.FrameReader);

            var payload = new ReadOnlyMemory<byte>([0x01, 0x02, 0x03]);

            var globalStopwatch = new Stopwatch();

            // Writer task
            var writerStopwatch = new Stopwatch();
            var writer = Task.Run(async () =>
            {
                globalStopwatch.Start();
                writerStopwatch.Start();
                for (int i = 0; i < FrameCount; i++)
                {
                    var frame = new NetworkFrame(
                        kind: NetworkFrameKind.Request,
                        eventType: null,
                        requestId: (uint)(i + 1),
                        streamId: null,
                        payload: payload);
                    await clientAdapter.WriteFrameAsync(frame, TestContext.CancellationToken);
                }
                writerStopwatch.Stop();
                await clientToServer.Writer.CompleteAsync();
            }, TestContext.CancellationToken);

            // Reader task
            var readerStopwatch = new Stopwatch();
            var buffer = new byte[64 * 1024];
            while (true)
            {
                var bytesRead =
                    await serverConnection.ReadAsync(buffer);

                if (bytesRead == 0)
                {
                    break;
                }

                await serverPipeline.RootDecoder
                    .DecodeFrameAsync(
                        new ReadOnlySequence<byte>(buffer, 0, bytesRead),
                        serverPipeline.FrameReader);
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

        [TestMethod]
        public async Task Layer3_Protocol_PipePerfTest_NonBlocking()
        {
            const int FrameCount = 100_000;

            var logger = NullLogger.Instance;

            //var (logger, loggerFactory) = DebugLoggerFactory.CreateLogger();
            //using var loggerScope = logger.EnterMethod(this);
            //logger.LogDebug("TEST: If you see this, the logger itself works");

            // -------------------------------------------------
            // Transport (pipes)
            // -------------------------------------------------
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

            // -------------------------------------------------
            // Build sessions (but DO NOT start them yet)
            // -------------------------------------------------
            var clientSession = new ProtocolSessionBuilder()
                .WithLogger(logger)
                .UseEvenStreamIds()
                .ConfigurePipeline(
                    pipeline =>
                    {
                        pipeline
                            .AppendFrameCodec(
                                new LengthPrefixedFrameEncoder(logger),
                                new LengthPrefixedFrameDecoder(logger))
                            .UseConnection(() => clientConnection);
                    })
                .Build();

            var serverSession = new ProtocolSessionBuilder()
                .WithLogger(logger)
                .UseOddStreamIds()
                .ConfigurePipeline(
                    pipeline =>
                    {
                        pipeline
                            .AppendFrameCodec(
                                new LengthPrefixedFrameEncoder(logger),
                                new LengthPrefixedFrameDecoder(logger))
                            .UseConnection(() => serverConnection);
                    })
                .Build();

            var payload = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 });
            var globalStopwatch = new Stopwatch();

            // =================================================
            // PHASE 1: ENQUEUE ONLY
            // =================================================

            // IMPORTANT:
            // No protocol loops running.
            // This measures PURE enqueue cost.
            var writerStopwatch = Stopwatch.StartNew();
            for (int i = 0; i < FrameCount; i++)
            {
                clientSession.Commands.SendEvent(1, payload);
            }
            writerStopwatch.Stop();

            // =================================================
            // PHASE 2: DRAIN ONLY
            // =================================================
            var receivedCount = 0;
            var allReceived = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);

            Stopwatch? readerStopwatch = null;
            serverSession.Observer.EventReceived += (_, _) =>
            {
                readerStopwatch ??= Stopwatch.StartNew();
                if (Interlocked.Increment(ref receivedCount) == FrameCount)
                {
                    allReceived.TrySetResult();
                }
            };

            // start the protocol loops
            // (wait within a maximum timeout so the test fails rather than hangs forever)
            using var lifecycleCts = new CancellationTokenSource();
            var serverRun = serverSession.Lifecycle.StartAsync(lifecycleCts.Token);
            var clientRun = clientSession.Lifecycle.StartAsync(lifecycleCts.Token);
            await Task
                .WhenAll(
                    serverSession.Lifecycle.Ready,
                    clientSession.Lifecycle.Ready)
                .WaitAsync(TimeSpan.FromSeconds(10), cancellationToken: default);

            // wait for messages to be dequeued
            // (wait within a maximum timeout so the test fails rather than hangs forever)
            await allReceived.Task
                .WaitAsync(TimeSpan.FromSeconds(10), cancellationToken: default);
            readerStopwatch?.Stop();

            // shut down cleanly
            // (wait within a maximum timeout so the test fails rather than hangs forever)
            lifecycleCts.Cancel();
            await Task
                .WhenAll(serverRun, clientRun)
                .WaitAsync(TimeSpan.FromSeconds(10), cancellationToken: default);

            globalStopwatch.Stop();

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
