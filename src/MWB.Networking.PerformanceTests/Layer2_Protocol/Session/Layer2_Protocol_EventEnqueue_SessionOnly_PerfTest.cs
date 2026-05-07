using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer3_Endpoint.Hosting;
using System.Diagnostics;

namespace Layer2_Protocol;

[TestClass]
public sealed class Session
{
    public TestContext TestContext
    {
        get;
        set;
    }

    //[AssemblyInitialize]
    //public static void AssemblyInit(TestContext context)
    //{
    //    TaskScheduler.UnobservedTaskException += (sender, e) =>
    //    {
    //        // Break into debugger immediately
    //        Debugger.Break();

    //        // Or force test run to fail hard
    //        e.SetObserved();
    //        throw e.Exception;
    //    };
    //}

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
    public Task Layer2_Protocol_EventEnqueue_SessionOnly_PerfTest()
    {
        const int FrameCount = 1_000_000;

        var logger = NullLogger.Instance;

        // ------------------------------------------------------------
        // Arrange: endpoint WITHOUT starting a session
        // ------------------------------------------------------------

        var endpoint =
            new SessionEndpointBuilder()
                .UseLogger(logger)
                .UseEvenStreamIds()
                .ConfigurePipelineWith(
                    pipeline =>
                    {
                        pipeline
                            .UseLogger(logger)
                            .UseLengthPrefixedCodec(logger)
                            .UseNullConnectionProvider(logger);
                    }
                )
                .Build();

        var payload = new ReadOnlyMemory<byte>(
            new byte[] { 0x01, 0x02, 0x03 });

        // ------------------------------------------------------------
        // Act: enqueue events (session not started)
        // ------------------------------------------------------------

        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < FrameCount; i++)
        {
            endpoint.SendEvent(
                eventType: 1,
                payload: payload);
        }

        stopwatch.Stop();

        // ------------------------------------------------------------
        // Report
        // ------------------------------------------------------------

        TestContext.WriteLine(
            $"[Layer2] Enqueued {FrameCount:N0} events (session-only) in " +
            $"{stopwatch.Elapsed.TotalMilliseconds:F2} ms " +
            $"({FrameCount / stopwatch.Elapsed.TotalSeconds:N0} events/sec)");

        return Task.CompletedTask;
    }
}
