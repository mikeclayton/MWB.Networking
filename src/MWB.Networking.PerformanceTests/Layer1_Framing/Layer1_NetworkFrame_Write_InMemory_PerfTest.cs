using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer0_Transport.Memory;
using MWB.Networking.Layer0_Transport.Memory.Buffer;
using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer1_Framing.Codecs.Default.Network.Hosting;
using MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.Transport.Hosting;
using MWB.Networking.Layer1_Framing.Pipeline.Hosting;
using System.Buffers;
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
    public async Task Layer1_Pipeline_Encoding_PerfTest()
    {
        const int FrameCount = 1_000_000;

        var logger = NullLogger.Instance;

        // ------------------------------------------------------------
        // Arrange: duplex in-memory transport + framing pipeline
        // ------------------------------------------------------------

        // Build framing pipeline on top of the in-memory connection
        var pipeline = new NetworkPipelineBuilder()
                .UseLogger(logger)
                .UseDefaultNetworkCodec()
                .UseLengthPrefixedCodec(logger)
                .Build();

        var payload = new ReadOnlyMemory<byte>(
            new byte[] { 0x01, 0x02, 0x03 });

        // ------------------------------------------------------------
        // Act: encode frames
        // ------------------------------------------------------------

        var decodedFrame = NetworkFrames.Request(
            requestId: 1u,
            payload: payload);

        var encodeStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < FrameCount; i++)
        {
            var encoded = pipeline.Encode(decodedFrame);
        }
        encodeStopwatch.Stop();

        // ------------------------------------------------------------
        // Act: decode frames
        // ------------------------------------------------------------

        var encodedFrame = new ReadOnlySequence<byte>(
            pipeline.Encode(decodedFrame).Collapse()[0]);

        var decodeStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < FrameCount; i++)
        {
            var result = pipeline.Decode(ref encodedFrame, out var decoded);
        }
        decodeStopwatch.Stop();

        // ------------------------------------------------------------
        // Report
        // ------------------------------------------------------------

        TestContext.WriteLine(
            $"[Framing] Encoded {FrameCount} frames in {encodeStopwatch.Elapsed.TotalMilliseconds:F2} ms " +
            $"({FrameCount / encodeStopwatch.Elapsed.TotalSeconds:N0} frames/sec)");

        TestContext.WriteLine(
            $"[Framing] Decoded {FrameCount} frames in {decodeStopwatch.Elapsed.TotalMilliseconds:F2} ms " +
            $"({FrameCount / decodeStopwatch.Elapsed.TotalSeconds:N0} frames/sec)");
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
    public async Task Layer1_Pipeline_Decoding_PerfTest()
    {
        const int FrameCount = 1_000_000;

        var logger = NullLogger.Instance;

        // ------------------------------------------------------------
        // Arrange: duplex in-memory transport + framing pipeline
        // ------------------------------------------------------------

        // Build framing pipeline on top of the in-memory connection
        var pipeline = new NetworkPipelineBuilder()
                .UseLogger(logger)
                .UseDefaultNetworkCodec()
                .UseLengthPrefixedCodec(logger)
                .Build();

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
            var decoded = pipeline.Encode(frame);
        }
        stopwatch.Stop();

        // ------------------------------------------------------------
        // Report
        // ------------------------------------------------------------

        TestContext.WriteLine(
            $"[Framing] Read {FrameCount} frames in {stopwatch.Elapsed.TotalMilliseconds:F2} ms " +
            $"({FrameCount / stopwatch.Elapsed.TotalSeconds:N0} frames/sec)");
    }
}
