using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

namespace _ProtocolSession;

/// <summary>
/// Tests for the request lifecycle: Request → Response* → Complete | Error | Cancel.
/// Covers snapshot state, outbound frame emission, and protocol violation guards.
/// </summary>
[TestClass]
public sealed partial class Requests_Invariants
{
    public TestContext TestContext
    {
        get;
        set;
    }

    // ---------------------------------------------------------------
    // Protocol violations
    // ---------------------------------------------------------------

    [TestMethod]
    public void Request_MissingRequestId_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        var frame = ProtocolFrameGenerator.CreateInvalidProtocolFrame(
            ProtocolFrameKind.Request);

        Assert.Throws<ProtocolException>(() => processor.ProcessFrame(frame));
    }

    [TestMethod]
    public void Response_MissingRequestId_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        var frame = ProtocolFrameGenerator.CreateInvalidProtocolFrame(
            ProtocolFrameKind.Response);

        Assert.Throws<ProtocolException>(() => processor.ProcessFrame(frame));
    }

    [TestMethod]
    public void Error_MissingRequestId_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        var frame = ProtocolFrameGenerator.CreateInvalidProtocolFrame(
            ProtocolFrameKind.Error);

        Assert.Throws<ProtocolException>(() => processor.ProcessFrame(frame));
    }

    [TestMethod]
    public void DuplicateRequestId_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));

        Assert.Throws<ProtocolException>(
            () => processor.ProcessFrame(ProtocolFrames.Request(1)));
    }

    [TestMethod]
    public void Response_UnknownRequestId_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        Assert.Throws<ProtocolException>(
            () => processor.ProcessFrame(ProtocolFrames.Response(99)));
    }

    [TestMethod]
    public void Error_UnknownRequestId_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        Assert.Throws<ProtocolException>(
            () => processor.ProcessFrame(ProtocolFrames.Error(99)));
    }

    [TestMethod]
    public void Response_AfterCompleteRequest_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.ProcessFrame(ProtocolFrames.Response(1));

        Assert.Throws<ProtocolException>(
            () => processor.ProcessFrame(ProtocolFrames.Response(1)));
    }

    [TestMethod]
    public void CompleteRequest_Twice_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.ProcessFrame(ProtocolFrames.Response(1));

        Assert.Throws<ProtocolException>(
            () => processor.ProcessFrame(ProtocolFrames.Response(1)));
    }
}
