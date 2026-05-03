using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

namespace _ProtocolSession;

/// <summary>
/// Tests for the request lifecycle: Request → Response* → Complete | Error | Cancel.
/// Covers snapshot state, outbound frame emission, and protocol violation guards.
/// </summary>
[TestClass]
public sealed partial class Streams_Inbound
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

    // ---------------------------------------------------------------
    // Streams - Inbound
    // ---------------------------------------------------------------

    [TestMethod]
    public void StreamOpen_AppearsInSnapshot()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(1));

        Assert.Contains(1u, session.Diagnostics.GetSnapshot().OpenStreams);
    }

    [TestMethod]
    public void StreamData_DoesNotCloseStream()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(1));
        processor.ProcessFrame(ProtocolFrames.StreamData(1, new byte[] { 0xAB }));

        Assert.Contains(1u, session.Diagnostics.GetSnapshot().OpenStreams);
    }

    [TestMethod]
    public void StreamData_MultipleFrames_StreamRemainsOpen()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(1));
        processor.ProcessFrame(ProtocolFrames.StreamData(1, new byte[] { 1 }));
        processor.ProcessFrame(ProtocolFrames.StreamData(1, new byte[] { 2 }));
        processor.ProcessFrame(ProtocolFrames.StreamData(1, new byte[] { 3 }));

        Assert.Contains(1u, session.Diagnostics.GetSnapshot().OpenStreams);
    }

    [TestMethod]
    public void StreamClose_RemovesStreamFromSnapshot()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(1));
        processor.ProcessFrame(ProtocolFrames.StreamClose(1));

        Assert.IsEmpty(session.Diagnostics.GetSnapshot().OpenStreams);
    }

    [TestMethod]
    public void MultipleConcurrentStreams_AllTrackedInSnapshot()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(10));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(20));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(30));

        var snap = session.Diagnostics.GetSnapshot();

        Assert.HasCount(3, snap.OpenStreams);
        Assert.Contains(10u, snap.OpenStreams);
        Assert.Contains(20u, snap.OpenStreams);
        Assert.Contains(30u, snap.OpenStreams);
    }

    [TestMethod]
    public void MultipleStreams_CloseIndependently()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(1));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.StreamClose(1));

        var snap = session.Diagnostics.GetSnapshot();

        Assert.HasCount(1, snap.OpenStreams);
        Assert.DoesNotContain(1u, snap.OpenStreams);
        Assert.Contains(2u, snap.OpenStreams);
    }

    [TestMethod]
    public void StreamId_ReusableAfterClose()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(1));
        processor.ProcessFrame(ProtocolFrames.StreamClose(1));

        // The same ID may be reused once the previous stream has closed.
        processor.ProcessFrame(ProtocolFrames.StreamOpen(1));

        Assert.Contains(1u, session.Diagnostics.GetSnapshot().OpenStreams);
    }

    [TestMethod]
    public void InboundStreamData_DoesNotEmitOutbound()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(1));
        processor.ProcessFrame(ProtocolFrames.StreamData(1, new byte[] { 10 }));
        processor.ProcessFrame(ProtocolFrames.StreamData(1, new byte[] { 20 }));

        Assert.IsEmpty(processor.DrainOutboundFrames());
    }


    [TestMethod]
    public void InboundStreamFrames_DoNotEmitOutbound()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(1));
        processor.ProcessFrame(ProtocolFrames.StreamData(1, new byte[] { 0x01 }));
        processor.ProcessFrame(ProtocolFrames.StreamClose(1));

        Assert.IsEmpty(processor.DrainOutboundFrames());
    }
}
