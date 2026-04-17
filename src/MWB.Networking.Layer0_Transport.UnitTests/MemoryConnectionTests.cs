using MWB.Networking.Hosting;
using MWB.Networking.Layer0_Transport.Memory;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed;
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
        public async Task MemoryPerfTest()
        {
            const int FrameCount = 1_000_000;

            // create one-way in-memory transport

            var pipeline = new NetworkPipelineBuilder()
                .AppendFrameCodec(
                    encoder: new LengthPrefixedFrameEncoder(),
                    decoder: new LengthPrefixedFrameDecoder())
                .UseConnection(() => new MemoryNetworkConnection(1024 * 1024 * 50))
                .Build();

            var adapter = new NetworkAdapter(
                pipeline.FrameWriter,
                pipeline.FrameReader);

            var payload = new ReadOnlyMemory<byte>([0x01, 0x02, 0x03]);

            // Writer task
            var writerStopwatch = new Stopwatch();
            await Task.Run(async () =>
            {
                writerStopwatch.Start();
                for (int i = 0; i < FrameCount; i++)
                {
                    var frame = new NetworkFrame(
                        kind: NetworkFrameKind.Request,
                        eventType: null,
                        requestId: (uint)(i + 1),
                        streamId: null,
                        payload: payload);
                    await adapter.WriteFrameAsync(frame, TestContext.CancellationToken);
                }
                writerStopwatch.Stop();
            }, TestContext.CancellationToken);

            TestContext.WriteLine(
                $"Wrote {FrameCount} frames in {writerStopwatch.Elapsed.TotalMilliseconds:F2} ms " +
                $"({FrameCount / writerStopwatch.Elapsed.TotalSeconds:N0} frames/sec)");
        }
    }
}
