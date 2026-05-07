using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer0_Transport.Pipes;
using MWB.Networking.Layer0_Transport.Stack.Lifecycle;
using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer1_Framing.Pipeline.Hosting;
using MWB.Networking.Layer2_Protocol.Session.Requests.Api;
using MWB.Networking.Layer3_Endpoint.Hosting;
using MWB.Networking.Logging;
using MWB.Networking.Logging.Debug;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;

namespace MWB.Networking.PerformanceTests;

[TestClass]
public sealed class Pipe_PerfTests_v2
{
    public TestContext TestContext
    {
        get;
        set;
    }

    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    [TestMethod]
    public async Task BasicRequestPayloadRoundtrip()
    {
        var (logger, _) = DebugLoggerFactory.CreateLogger();

        // simulates a duplex connection
        var serverPipe = new Pipe();
        var clientPipe = new Pipe();

        // ------------------------------------------------------------
        // Build server session
        // ------------------------------------------------------------
        var serverStatus = new ObservableConnectionStatus();

        using var serverConnection = new PipeNetworkConnection(
            logger: logger,
            reader: serverPipe.Reader,
            writer: clientPipe.Writer,
            status: serverStatus);

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
        var clientStatus = new ObservableConnectionStatus();

        var clientConnection = new PipeNetworkConnection(
            logger: logger,
            reader: clientPipe.Reader,
            writer: serverPipe.Writer,
            status: clientStatus);

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
        using var lifecycleCts = new CancellationTokenSource();

        await serverEndpoint
            .StartAsync(lifecycleCts.Token)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);

        await clientEndpoint
            .StartAsync(lifecycleCts.Token)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);

        // ------------------------------------------------------------
        // Act: send a frame from client to server
        // ------------------------------------------------------------
        var payload = new byte[] { 0x01, 0x02, 0x03 };

        clientEndpoint.SendRequest(payload);

        // ------------------------------------------------------------
        // Assert: server receives the request
        // ------------------------------------------------------------
        var (_, receivedPayload) =
            await requestTcs.Task
                .WaitAsync(TestContext.CancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);

        CollectionAssert.AreEqual(payload, receivedPayload.ToArray());

        // ------------------------------------------------------------
        // Clean shutdown
        // ------------------------------------------------------------
        lifecycleCts.Cancel();

        await Task
            .WhenAll(
                serverEndpoint.DisposeAsync().AsTask(),
                clientEndpoint.DisposeAsync().AsTask())
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Layer1_Framing_PipePerfTest_NonBlocking()
    {
        const int FrameCount = 100_000;

        var logger = NullLogger.Instance;

        using var loggerScope = logger.BeginMethodLoggingScope(this);

        // create duplex in-memory transport (pipes cross-wired)
        var clientToServer = new Pipe(new PipeOptions(
            pauseWriterThreshold: 1024 * 1024 * 50,
            resumeWriterThreshold: 1024 * 1024 * 50));
        var serverToClient = new Pipe();

        var clientStatus = new ObservableConnectionStatus();
        var clientConnection = new PipeNetworkConnection(
            logger: logger,
            reader: serverToClient.Reader,
            writer: clientToServer.Writer,
            status: clientStatus);

        var serverStatus = new ObservableConnectionStatus();
        var serverConnection = new PipeNetworkConnection(
            logger: logger,
            reader: clientToServer.Reader,
            writer: serverToClient.Writer,
            status: serverStatus);

        // ----------------------------
        // Build client pipeline
        // ----------------------------
        var clientPipeline = await new NetworkPipelineBuilder()
            .UseLogger(logger)
            .UseLengthPrefixedCodec(logger)
            .WrapConnectionAsProvider(logger, clientConnection)
            .CreatePipelineAsync(TestContext.CancellationToken);

        // ----------------------------
        // Build server pipeline
        // ----------------------------
        var serverPipeline = await new NetworkPipelineBuilder()
            .UseLogger(logger)
            .UseLengthPrefixedCodec(logger)
            .WrapConnectionAsProvider(logger, serverConnection)
            .CreatePipelineAsync(TestContext.CancellationToken);

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
                await clientPipeline.WriteFrameAsync(frame, TestContext.CancellationToken);
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

            await serverPipeline
                .DecodeFrameAsync(
                    new ReadOnlySequence<byte>(buffer, 0, bytesRead),
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
    /// just with 100,000 events as a performance test rather than 3 events for a
    /// correctness test.
    /// </remarks>
    [TestMethod]
    public async Task Layer2_Protocol_PipePerfTest_NonBlocking()
    {
        const int FrameCount = 100_000;

        var logger = NullLogger.Instance;

        // -------------------------------------------------
        // Transport (in-memory pipes)
        // -------------------------------------------------
        var clientToServer = new Pipe();
        var serverToClient = new Pipe();

        var clientStatus = new ObservableConnectionStatus();
        var clientConnection = new PipeNetworkConnection(
            logger: logger,
            reader: serverToClient.Reader,
            writer: clientToServer.Writer,
            status: clientStatus);

        var serverStatus = new ObservableConnectionStatus();
        var serverConnection = new PipeNetworkConnection(
            logger: logger,
            reader: clientToServer.Reader,
            writer: serverToClient.Writer,
            status: serverStatus);

        // -------------------------------------------------
        // Build sessions (NOT started yet)
        // -------------------------------------------------
        var clientEndpoint = new SessionEndpointBuilder()
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

        var received = 0;
        var allReceived = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var readerStopwatch = (Stopwatch?)null;

        var serverEndpoint = new SessionEndpointBuilder()
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
        // PHASE 1: ENQUEUE (non-blocking)
        // =================================================
        var globalStopwatch = new Stopwatch();
        var writerStopwatch = new Stopwatch();
        for (var i = 0; i < FrameCount; i++)
        {
            clientEndpoint.SendEvent(1, payload);
        }
        writerStopwatch.Stop();

        // -------------------------------------------------
        // PHASE 2: Start sessions
        // -------------------------------------------------
        using var lifecycleCts = new CancellationTokenSource();

        await serverEndpoint
            .StartAsync(lifecycleCts.Token)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);

        await clientEndpoint
            .StartAsync(lifecycleCts.Token)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);

        // -------------------------------------------------
        // Assert: pre-start messages are delivered
        // -------------------------------------------------
        await allReceived.Task
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);
        readerStopwatch?.Stop();
        globalStopwatch.Stop();

        Assert.AreEqual(FrameCount, received);

        // ------------------------------------------------------------
        // Clean shutdown
        // ------------------------------------------------------------
        lifecycleCts.Cancel();

        await Task
            .WhenAll(
                serverEndpoint.DisposeAsync().AsTask(),
                clientEndpoint.DisposeAsync().AsTask())
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
