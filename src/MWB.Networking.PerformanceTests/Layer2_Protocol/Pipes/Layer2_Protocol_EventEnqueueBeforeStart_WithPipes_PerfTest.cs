using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer0_Transport.Pipes;
using MWB.Networking.Layer0_Transport.Stack.Lifecycle;
using MWB.Networking.Layer1_Framing.Codecs.Default.Network;
using MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.Transport;
using MWB.Networking.Layer3_Endpoint.Hosting;
using System.Diagnostics;
using System.IO.Pipelines;

namespace Layer2_Protocol;

public sealed partial class Pipes
{
    /// <remarks>
    /// This is identical to Layer2_Protocol_SendBeforeStart_IsDeliveredAfterStart,
    /// just wth 100_000 events as a performance test rather than 3 events for a
    /// correctness test. We could probably make the number of frames a test input
    /// and run both tests with the same code.
    /// </remarks>
    [TestMethod]
    public async Task Layer2_Protocol_EventEnqueueBeforeStart_WithPipes_PerfTest()
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
            logger,
            reader: serverToClient.Reader,
            writer: clientToServer.Writer,
            status: new ObservableConnectionStatus());

        var serverConnection = new PipeNetworkConnection(
            logger,
            reader: clientToServer.Reader,
            writer: serverToClient.Writer,
            status: new ObservableConnectionStatus());

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
                        .UseDefaultNetworkCodec()
                        .UseLengthPrefixedCodec(logger)
                        .WrapConnectionAsProvider(logger, clientConnection);
                }
            )
            .Build();

        // used in observer.EventReceived
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
                        .UseDefaultNetworkCodec()
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
        // PHASE 1: ENQUEUE (non‑blocking)
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
            .WhenAll(
                serverEndpoint.DisposeAsync().AsTask(),
                clientEndpoint.DisposeAsync().AsTask())
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);

        // ------------------------------------------------------------
        // Log statistics
        // ------------------------------------------------------------

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
