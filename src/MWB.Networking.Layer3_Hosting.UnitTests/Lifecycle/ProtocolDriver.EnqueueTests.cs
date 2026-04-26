using MWB.Networking.Layer0_Transport.Pipes;
using MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed.Hosting;
using MWB.Networking.Layer1_Framing.Hosting.Manual;
using MWB.Networking.Layer3_Hosting.Configuration;
using MWB.Networking.Logging.Loggers;
using System.Diagnostics;
using System.IO.Pipelines;

namespace _ProtocolDriver;

[TestClass]
public sealed partial class EndToEnd
{
    public TestContext TestContext
    {
        get;
        set;
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

        var (logger, _) = DebugLoggerFactory.Create();

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
                    .UseManualNetworkConnectionProvider(logger, clientConnection);
            })
            .Build();

        var serverSession = new SessionHostBuilder()
            .WithLogger(logger)
            .UseOddStreamIds()
            .ConfigurePipeline(pipeline =>
            {
                pipeline
                    .UseLengthPrefixedCodec(logger)
                    .UseManualNetworkConnectionProvider(logger, serverConnection);
            })
            .Build();

        var payload = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 });

        // -------------------------------------------------
        // PHASE 1: Send BEFORE start
        // -------------------------------------------------
        for (int i = 0; i < FrameCount; i++)
        {
            clientSession.Commands.SendEvent(1, payload);
        }

        // -------------------------------------------------
        // Observe received events
        // -------------------------------------------------
        var received = 0;
        var allReceived = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var readerStopwatch = (Stopwatch?)null;
        serverSession.Observers.EventReceived += (_, _) =>
        {
            readerStopwatch ??= Stopwatch.StartNew();
            if (Interlocked.Increment(ref received) == FrameCount)
            {
                allReceived.TrySetResult();
            }
        };

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
