using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Session.Frames;
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

        // Send an outgoing request so the peer can legitimately respond to it.
        var outgoing = session.Commands.SendRequest();
        // First response closes the request.
        processor.ProcessFrame(ProtocolFrames.Response(outgoing.RequestId));
        // Second response for the now-closed (and removed) request must be rejected.
        Assert.Throws<ProtocolException>(
            () => processor.ProcessFrame(ProtocolFrames.Response(outgoing.RequestId)));
    }

    // ---------------------------------------------------------------
    // Directionality enforcement (B2)
    // ---------------------------------------------------------------

    [TestMethod]
    public void Response_TargetingInboundRequest_ThrowsProtocolException()
    {
        // A Response frame must only close an *outgoing* request (one we sent).
        // If the peer sends us a Response whose ID matches an *inbound* request
        // (one they sent to us), we must reject it. Silently closing the inbound
        // entry would make that request unresolvable and free the ID prematurely.
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        // Peer sends an inbound request with ID 1.
        processor.ProcessFrame(ProtocolFrames.Request(1));

        // Peer then (incorrectly) sends a Response for the same ID.
        // This must be rejected because ID 1 is tracked as an incoming request,
        // not as an outgoing one that we're expecting a response to.
        Assert.Throws<ProtocolException>(
            () => processor.ProcessFrame(ProtocolFrames.Response(1)));
    }

    [TestMethod]
    public void Error_TargetingInboundRequest_ThrowsProtocolException()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));

        Assert.Throws<ProtocolException>(
            () => processor.ProcessFrame(ProtocolFrames.Error(1)));
    }
}
