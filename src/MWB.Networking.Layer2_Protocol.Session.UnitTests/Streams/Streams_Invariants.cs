using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Session.Frames;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

namespace _ProtocolSession;

/// <summary>
/// Tests for the request lifecycle: Request → Response* → Complete | Error | Cancel.
/// Covers snapshot state, outbound frame emission, and protocol violation guards.
/// </summary>
[TestClass]
public sealed partial class Streams_Invariants
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
    // Streams - Invariants
    // ---------------------------------------------------------------

    [TestMethod]
    public void StreamOpen_MissingStreamId_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        var frame = ProtocolFrameGenerator.CreateInvalidProtocolFrame(
            ProtocolFrameKind.StreamOpen);

        Assert.Throws<ProtocolException>(() => processor.ProcessFrame(frame));
    }

    [TestMethod]
    public void DuplicateStreamId_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(1));

        Assert.Throws<ProtocolException>(
            () => processor.ProcessFrame(ProtocolFrames.StreamOpen(1)));
    }

    [TestMethod]
    public void StreamData_UnknownStreamId_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        Assert.Throws<ProtocolException>(
            () => processor.ProcessFrame(ProtocolFrames.StreamData(99, new byte[] { 1 })));
    }

    [TestMethod]
    public void StreamClose_UnknownStreamId_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        Assert.Throws<ProtocolException>(
            () => processor.ProcessFrame(ProtocolFrames.StreamClose(99)));
    }

    [TestMethod]
    public void StreamData_MissingStreamId_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        var frame = ProtocolFrameGenerator.CreateInvalidProtocolFrame(
            ProtocolFrameKind.StreamData);

        Assert.Throws<ProtocolException>(() => processor.ProcessFrame(frame));
    }

    [TestMethod]
    public void StreamData_AfterClose_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(1));
        processor.ProcessFrame(ProtocolFrames.StreamClose(1));

        Assert.Throws<ProtocolException>(
            () => processor.ProcessFrame(ProtocolFrames.StreamData(1, new byte[] { 1 })));
    }

    [TestMethod]
    public void StreamClose_Twice_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(1));
        processor.ProcessFrame(ProtocolFrames.StreamClose(1));

        // Stream is removed after close, so the second close hits an unknown-id error.
        Assert.Throws<ProtocolException>(
            () => processor.ProcessFrame(ProtocolFrames.StreamClose(1)));
    }
}
