using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Hosting;
using MWB.Networking.Layer0_Transport.Memory;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed;
using MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed.Hosting;
using System.Diagnostics;

namespace MWB.Networking.Layer0_Transport.UnitTests;

public class MemoryConnectionTests
{
    [TestClass]
    public sealed class SmokeTests
    {
        public TestContext TestContext
        {
            get;
            set;
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
        public async Task NetworkFramePerfTest()
        {
            const int FrameCount = 1_000_000;

            var logger = NullLogger.Instance;

            // ------------------------------------------------------------
            // Arrange: duplex in-memory transport + framing pipeline
            // ------------------------------------------------------------

            var (providerA, providerB) =
                InMemoryNetworkConnectionProvider.CreateDuplexProviders();

            // We'll write frames from A; B is unused in this test
            var connectionA =
                await providerA.OpenConnectionAsync(TestContext.CancellationToken);

            // Build framing pipeline on top of the in-memory connection
            var pipeline =
                new NetworkPipelineBuilder()
                    .UseLengthPrefixedCodec(logger)
                    .UseConnection(() => connectionA)
                    .Build();

            var adapter =
                new NetworkAdapter(
                    logger,
                    pipeline.FrameWriter,
                    pipeline.FrameReader);

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

                await adapter.WriteFrameAsync(
                    frame,
                    TestContext.CancellationToken);
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
}
