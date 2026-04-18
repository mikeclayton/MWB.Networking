using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;
using System.Diagnostics;

namespace MWB.Networking.Layer2_Protocol.UnitTests;

public partial class ProtocolSessionTests
{
    /// <summary>
    /// Throughput tests that measure wall-clock time for high-volume frame processing.
    /// No hard timing assertions are made — results are written to the test output for
    /// comparison across runs. Each test also validates that the session state is correct
    /// after the run to ensure no frames were silently dropped.
    /// </summary>
    [TestClass]
    public sealed partial class Performance
    {
        public TestContext TestContext
        {
            get;
            set;
        }

        private static readonly byte[] FourBytes = [0x01, 0x02, 0x03, 0x04];

        private const int Iterations = 10_000;
        private const int WarmupIterations = 200;

        // ---------------------------------------------------------------
        // Events
        // ---------------------------------------------------------------

        [TestMethod]
        public void Performance_Events_10000()
        {
            var session = ProtocolSessionHelper.CreateNullSession();
            var runtime = session.Runtime;

            // Warm up the JIT and dictionary internals before timing.
            for (var i = 0; i < WarmupIterations; i++)
            {
                runtime.ProcessFrame(ProtocolFrames.Event(1, FourBytes));
            }
            runtime.DrainOutboundFrames();

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < Iterations; i++)
            {
                runtime.ProcessFrame(ProtocolFrames.Event(1, FourBytes));
            }
            sw.Stop();

            // Drain after timing so outbound allocation doesn't skew the measurement.
            var outbound = runtime.DrainOutboundFrames();

            // Verify no frames were output for events.
            Assert.HasCount(0, outbound);
            Assert.IsEmpty(session.Diagnostics.GetSnapshot().OpenRequests);
            Assert.IsEmpty(session.Diagnostics.GetSnapshot().OpenStreams);

            Report("Events", sw, Iterations);
        }

        // ---------------------------------------------------------------
        // Requests
        // ---------------------------------------------------------------

        [TestMethod]
        public void Performance_Requests_10000()
        {
            var session = ProtocolSessionHelper.CreateNullSession();
            var runtime = session.Runtime;

            // Reuse a single request ID per iteration: Complete removes it so it
            // can be reused immediately, keeping dictionary size constant at 0-1.
            const uint Id = 1;

            for (var i = 0; i < WarmupIterations; i++)
            {
                runtime.ProcessFrame(ProtocolFrames.Request(Id));
                runtime.ProcessFrame(ProtocolFrames.Response(Id));
            }
            runtime.DrainOutboundFrames();

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < Iterations; i++)
            {
                runtime.ProcessFrame(ProtocolFrames.Request(Id));
                runtime.ProcessFrame(ProtocolFrames.Response(Id));
            }
            sw.Stop();

            runtime.DrainOutboundFrames();

            // Session should be fully quiesced: no open requests remain.
            Assert.IsEmpty(session.Diagnostics.GetSnapshot().OpenRequests);

            Report("Requests (open + complete)", sw, Iterations);
        }

        // ---------------------------------------------------------------
        // Streams
        // ---------------------------------------------------------------

        [TestMethod]
        public void Performance_Streams_OpenSendClose_10000()
        {
            var session = ProtocolSessionHelper.CreateNullSession();
            var runtime = session.Runtime;

            // Reuse stream ID 1: StreamClose removes it so it can be reused.
            const uint Id = 1;

            for (var i = 0; i < WarmupIterations; i++)
            {
                runtime.ProcessFrame(ProtocolFrames.StreamOpen(Id));
                runtime.ProcessFrame(ProtocolFrames.StreamData(Id, FourBytes));
                runtime.ProcessFrame(ProtocolFrames.StreamClose(Id));
            }
            runtime.DrainOutboundFrames();

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < Iterations; i++)
            {
                runtime.ProcessFrame(ProtocolFrames.StreamOpen(Id));
                runtime.ProcessFrame(ProtocolFrames.StreamData(Id, FourBytes));
                runtime.ProcessFrame(ProtocolFrames.StreamClose(Id));
            }
            sw.Stop();

            runtime.DrainOutboundFrames();

            // Session should be fully quiesced: no open streams remain.
            Assert.IsEmpty(session.Diagnostics.GetSnapshot().OpenStreams);

            Report("Streams (open + 4-byte data + close)", sw, Iterations);
        }

        // ---------------------------------------------------------------
        // Helper
        // ---------------------------------------------------------------

        private void Report(string label, Stopwatch sw, int count)
        {
            var totalMs = sw.Elapsed.TotalMilliseconds;
            var usPerOp = sw.Elapsed.TotalMicroseconds / count;
            TestContext.WriteLine($"{label}: {count:N0} iterations in {totalMs:F2} ms ({usPerOp:F2} µs/op)");
        }
    }
}
