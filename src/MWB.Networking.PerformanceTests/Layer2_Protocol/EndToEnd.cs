using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer0_Transport.Memory;
using MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed.Hosting;
using MWB.Networking.Layer3_Hosting.Configuration;
using MWB.Networking.Logging;
using System.Diagnostics;

namespace Performance;

[TestClass]
public partial class Layer2_Protocol_EndToEnd
{

    public TestContext TestContext
    {
        get;
        set;
    }

    /// <remarks>
    /// This is similar to Layer2_Protocol_SendBeforeStart_IsDeliveredAfterStart,
    /// just wth 100_000 events as a performance test rather than 3 events for a
    /// correctness test.
    /// </remarks>
    [TestMethod]
    public async Task Layer2_Protocol_SendBeforeStart_PerformanceTest()
    {
        const int FrameCount = 100_000;

        var logger = NullLogger.Instance;
        //var (logger, loggerFactory) = DebugLoggerFactory.CreateLogger();
        using var loggerScope = logger.BeginMethodLoggingScope(this);
        logger.LogDebug("TEST: If you see this, the logger itself works");
        logger.LogDebug(nameof(Layer2_Protocol_SendBeforeStart_PerformanceTest));

        // -------------------------------------------------
        // Transport (duplex in-memory)
        // -------------------------------------------------

        var (providerA, providerB) =
            InMemoryNetworkConnectionProvider.CreateDuplexProviders(logger);

        // ----------------------------
        // Build session A (ritwe)
        // ----------------------------

        var sessionA = new SessionHostBuilder()
            .WithLogger(logger)
            .UseOddStreamIds()
            .ConfigurePipeline(pipeline =>
            {
                pipeline
                    .UseLengthPrefixedCodec(logger)
                    .UseConnectionProvider(providerA);
            })
            .Build();

        // ----------------------------
        // Build session B (reader)
        // ----------------------------

        var received = 0;
        var allReceived = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        Stopwatch? readerStopwatch = default;
        var sessionB = new SessionHostBuilder()
            .WithLogger(logger)
            .UseEvenStreamIds()
            .ConfigurePipeline(pipeline =>
            {
                pipeline
                    .UseLengthPrefixedCodec(logger)
                    .UseConnectionProvider(providerB);
            })
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

        // ---------------------------------------------
        // PHASE 1: enqueue all outbound events
        // ---------------------------------------------

        var payload = new ReadOnlyMemory<byte>([0x01, 0x02, 0x03]);
        var globalStopwatch = Stopwatch.StartNew();
        var writerStopwatch = Stopwatch.StartNew();
        for (var i = 0; i < FrameCount; i++)
        {
            sessionA.Commands.SendEvent(
                eventType: (uint)i,
                payload: payload);
        }
        writerStopwatch.Stop();

        // ---------------------------------------------
        // PHASE 2: start sessions (read loops begin)
        // ---------------------------------------------
        var lifecycleCts = new CancellationTokenSource();
        var runA = sessionA.StartAsync(lifecycleCts.Token);
        var runB = sessionB.StartAsync(lifecycleCts.Token);

        await Task
            .WhenAll(sessionA.WhenReady, sessionB.WhenReady)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);

        // ---------------------------------------------
        // PHASE 3: wait for completion
        // ---------------------------------------------

        await allReceived.Task
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);
        readerStopwatch?.Stop();
        globalStopwatch.Stop();

        // ---------------------------------------------
        // PHASE 3: log statistics
        // ---------------------------------------------

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
