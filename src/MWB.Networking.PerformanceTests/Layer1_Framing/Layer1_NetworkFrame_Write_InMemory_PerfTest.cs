using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer0_Transport.Memory;
using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer1_Framing.Pipeline.Hosting;
using MWB.Networking.PerformanceTests;
using System.Diagnostics;

namespace Layer1_Framing;

[TestClass]
public sealed partial class Memory
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
    /// NOTE: This is a best-case, in-memory framing throughput test.
    /// It intentionally avoids IO, backpressure, and allocation pressure.
    /// The goal is to establish an upper bound on framing overhead.
    ///
    /// In tests, it's hit upwards of 2.5 million frames per second -
    /// real-world usage across a network will obviously be slower, but
    /// it demonstrates that Layer 0 and framing is not a bottleneck.
    /// </summary>
    [TestMethod]
    public async Task Layer1_NetworkFrame_Write_InMemory_PerfTest()
    {
        const int FrameCount = 1_000_000;

        var logger = NullLogger.Instance;

        // ------------------------------------------------------------
        // Arrange: duplex in-memory transport + framing pipeline
        // ------------------------------------------------------------

        // We'll write frames from A; B is unused in this test
        var (providerA, providerB) =
            InMemoryNetworkConnectionProvider.CreateDuplexProviders(logger);

        // Build framing pipeline on top of the in-memory connection
        var pipeline =
            await new NetworkPipelineBuilder()
                .UseLogger(logger)
                .UseLengthPrefixedCodec(logger)
                .UseConnectionProvider(providerA)
                .CreatePipelineAsync(TestContext.CancellationToken);

        var payload = new ReadOnlyMemory<byte>(
            new byte[] { 0x01, 0x02, 0x03 });

        // ------------------------------------------------------------
        // Act: write frames
        // ------------------------------------------------------------

        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < FrameCount; i++)
        {
            var frame = NetworkFrames.Request(
                requestId: (uint)(i + 1),
                payload: payload);
            await pipeline
                .WriteFrameAsync(frame, TestContext.CancellationToken)
                .AsTask()
                .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);
        }
        stopwatch.Stop();

        // ------------------------------------------------------------
        // Report
        // ------------------------------------------------------------

        TestContext.WriteLine(
            $"[Framing] Wrote {FrameCount} frames in {stopwatch.Elapsed.TotalMilliseconds:F2} ms " +
            $"({FrameCount / stopwatch.Elapsed.TotalSeconds:N0} frames/sec)");
    }
}
