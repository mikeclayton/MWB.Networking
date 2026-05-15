using MWB.Networking.Layer0_Transport.Pipes;
using MWB.Networking.Layer0_Transport.Stack.Lifecycle;
using MWB.Networking.Layer1_Framing.Codecs.Default.Network.Hosting;
using MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.Transport;
using MWB.Networking.Layer3_Endpoint.Hosting;
using MWB.Networking.Logging.Debug;
using System.Diagnostics;
using System.IO.Pipelines;

namespace _ProtocolDriver;

[TestClass]
public sealed class EnqueueTasks
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

    /// <summary>
    /// Runtime contract test: verifies that outbound messages sent
    /// <em>before</em> calling <c>ProtocolSession.Lifecycle.StartAsync</c>
    /// are not lost and are delivered once the session starts.
    ///
    /// This protects the guarantee that pre-start sends are buffered
    /// and drained by the write loop on startup.
    /// </summary>
    /// <remarks>
    /// Prevents a previous bug where messages enqueued with <c>SendEvent</c>, etc
    /// <em>before</em> calling <c>StartAsync</c> were silently dropped instead
    /// of being drained by the write loop once it was eventually started.
    /// </remarks>
    [TestMethod]
    public async Task Layer2_Protocol_SendBeforeStart_IsDeliveredAfterStart()
    {
        const int FrameCount = 3;

        var (logger, _) = DebugLoggerFactory.CreateLogger();

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
            .UseManualNetworkConnectionProvider(logger, clientConnection)
            .UsePipeline(pipeline =>
                pipeline
                    .UseLogger(logger)
                    .UseDefaultNetworkCodec()
                    .UseLengthPrefixedCodec(logger)
            )
            .Build();

        Stopwatch? readerStopwatch = null;
        var received = 0;
        var allReceived = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var serverEndpoint = new SessionEndpointBuilder()
            .UseLogger(logger)
            .UseOddStreamIds()
            .UseInstrumentedNetworkConnectionProvider(logger, serverConnection)
            .UsePipeline(pipeline =>
                pipeline
                    .UseLogger(logger)
                    .UseDefaultNetworkCodec()
                    .UseLengthPrefixedCodec(logger)
            )
            .OnEventReceived(
                (_, _) =>
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

        // -------------------------------------------------
        // PHASE 1: Send BEFORE start
        // -------------------------------------------------
        for (int i = 0; i < FrameCount; i++)
        {
            clientEndpoint.SendEvent(1, payload);
        }

        // -------------------------------------------------
        // PHASE 2: Start sessions
        // -------------------------------------------------

        // start the protocol loops
        // (wait within a maximum timeout so the test fails rather than hangs forever)
        using var lifecycleCts = new CancellationTokenSource();
        var serverRun = serverEndpoint.StartAsync(lifecycleCts.Token);
        var clientRun = clientEndpoint.StartAsync(lifecycleCts.Token);
        await Task
            .WhenAll(serverRun, clientRun)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);

        // -------------------------------------------------
        // Assert: pre-start messages are delivered
        // -------------------------------------------------
        // wait for messages to be dequeued
        // (wait within a maximum timeout so the test fails rather than hangs forever)
        await allReceived.Task
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);
        readerStopwatch?.Stop();

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
            $"Read {FrameCount} frames in {readerStopwatch?.Elapsed.TotalMilliseconds:F2} ms " +
            $"({FrameCount / readerStopwatch?.Elapsed.TotalSeconds:N0} frames/sec)");
    }
}
