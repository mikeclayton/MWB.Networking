using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.PerformanceTests.Helpers;
using System.Diagnostics;

namespace Performance;

/// <summary>
/// Throughput tests that measure wall-clock time for high-volume frame processing.
/// No hard timing assertions are made — results are written to the test output for
/// comparison across runs. Each test also validates that the session state is correct
/// after the run to ensure no frames were silently dropped.
/// </summary>
[TestClass]
public sealed partial class Layer2_Protocol_Events
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
    public void Layer2_Protocol_Performance_Events_10000()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
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

        Layer2_Protocol_Performance.Report(this.TestContext, "Events", sw, Iterations);
    }

    // ---------------------------------------------------------------
    // Requests
    // ---------------------------------------------------------------

    [TestMethod]
    public void Layer2_Protocol_Performance_Requests_10000()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
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

        Layer2_Protocol_Performance.Report(this.TestContext, "Requests (open + complete)", sw, Iterations);
    }

    // ---------------------------------------------------------------
    // Streams
    // ---------------------------------------------------------------

    [TestMethod]
    public void Layer2_Protocol_Performance_Streams_OpenSendClose_10000()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var runtime = session.Runtime;

        // Reuse stream ID 1: StreamClose removes it so it can be reused.
        const uint streamId = 1;

        for (var i = 0; i < WarmupIterations; i++)
        {
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(streamId));
            runtime.ProcessFrame(ProtocolFrames.StreamData(streamId, FourBytes));
            runtime.ProcessFrame(ProtocolFrames.StreamClose(streamId));
        }
        runtime.DrainOutboundFrames();

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < Iterations; i++)
        {
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(streamId));
            runtime.ProcessFrame(ProtocolFrames.StreamData(streamId, FourBytes));
            runtime.ProcessFrame(ProtocolFrames.StreamClose(streamId));
        }
        sw.Stop();

        runtime.DrainOutboundFrames();

        // Session should be fully quiesced: no open streams remain.
        Assert.IsEmpty(session.Diagnostics.GetSnapshot().OpenStreams);

        Layer2_Protocol_Performance.Report(this.TestContext, "Streams (open + 4-byte data + close)", sw, Iterations);
    }
}
