using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Hosting;
using MWB.Networking.Layer0_Transport.Pipes;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed;
using MWB.Networking.Layer2_Protocol.Requests.Api;
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
            var logger = NullLogger.Instance;

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
                    .WithLogger(NullLogger.Instance)
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
                    .WithLogger(NullLogger.Instance)
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

            var logger = NullLogger.Instance;

            // create duplex in-memory transport (pipes cross-wired)
            var clientToServer =new Pipe(new PipeOptions(
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
        public async Task PipePerfTestNonBlocking()
        {
            const int FrameCount = 100_000;

            var logger = NullLogger.Instance;

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
    }
}
