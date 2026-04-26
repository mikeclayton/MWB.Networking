using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Frames;
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

    // ---------------------------------------------------------------
    // Streams - Invariants
    // ---------------------------------------------------------------

    [TestMethod]
    public void StreamOpen_MissingStreamId_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var runtime = session.Runtime;

        var frame = ProtocolFrameGenerator.CreateInvalidProtocolFrame(
            ProtocolFrameKind.StreamOpen);

        Assert.Throws<ProtocolException>(() => runtime.ProcessFrame(frame));
    }

    [TestMethod]
    public void DuplicateStreamId_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var runtime = session.Runtime;

        runtime.ProcessFrame(ProtocolFrames.StreamOpen(1));

        Assert.Throws<ProtocolException>(
            () => runtime.ProcessFrame(ProtocolFrames.StreamOpen(1)));
    }

    [TestMethod]
    public void StreamData_UnknownStreamId_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var runtime = session.Runtime;

        Assert.Throws<ProtocolException>(
            () => runtime.ProcessFrame(ProtocolFrames.StreamData(99, new byte[] { 1 })));
    }

    [TestMethod]
    public void StreamClose_UnknownStreamId_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var runtime = session.Runtime;

        Assert.Throws<ProtocolException>(
            () => runtime.ProcessFrame(ProtocolFrames.StreamClose(99)));
    }

    [TestMethod]
    public void StreamData_MissingStreamId_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var runtime = session.Runtime;

        var frame = ProtocolFrameGenerator.CreateInvalidProtocolFrame(
            ProtocolFrameKind.StreamData);

        Assert.Throws<ProtocolException>(() => runtime.ProcessFrame(frame));
    }

    [TestMethod]
    public void StreamData_AfterClose_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var runtime = session.Runtime;

        runtime.ProcessFrame(ProtocolFrames.StreamOpen(1));
        runtime.ProcessFrame(ProtocolFrames.StreamClose(1));

        Assert.Throws<ProtocolException>(
            () => runtime.ProcessFrame(ProtocolFrames.StreamData(1, new byte[] { 1 })));
    }

    [TestMethod]
    public void StreamClose_Twice_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var runtime = session.Runtime;

        runtime.ProcessFrame(ProtocolFrames.StreamOpen(1));
        runtime.ProcessFrame(ProtocolFrames.StreamClose(1));

        Assert.Throws<ProtocolException>(
            () => runtime.ProcessFrame(ProtocolFrames.StreamClose(1)));
    }
}
