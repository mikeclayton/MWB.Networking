using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Session.Frames;
using MWB.Networking.Layer2_Protocol.Session.UnitTests.Helpers;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

namespace _ProtocolSession;

/// <summary>
/// Integration-level tests that exercise the full session across multiple
/// protocol sub-systems simultaneously: outbound capture semantics,
/// mixed request+stream lifecycles, and cross-subsystem isolation.
/// </summary>
[TestClass]
public sealed class Session_Integration
{
    public TestContext TestContext { get; set; }

    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    // ---------------------------------------------------------------
    // OutboundFrameCapture semantics
    // ---------------------------------------------------------------

    [TestMethod]
    public void OutboundCapture_IsEmptyBeforeAnyFramesProduced()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        Assert.IsEmpty(capture.Frames);
    }

    [TestMethod]
    public void OutboundCapture_Drain_ClearsAccumulatedFrames()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.SendEvent(1u);

        var first = capture.Drain();
        Assert.HasCount(1, first);
        Assert.IsEmpty(capture.Frames);
    }

    [TestMethod]
    public void OutboundCapture_AccumulatesFramesAcrossMultipleOperations()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.SendEvent(1u);
        session.Commands.SendEvent(2u);
        session.Commands.SendEvent(3u);

        Assert.HasCount(3, capture.Frames);
    }

    [TestMethod]
    public void OutboundCapture_AfterDrain_AccumulatesOnlyNewFrames()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.SendEvent(1u, new byte[] { 0x01 });
        capture.Drain();

        session.Commands.SendEvent(2u, new byte[] { 0x02 });
        session.Commands.SendEvent(3u, new byte[] { 0x03 });

        var frames = capture.Frames;
        Assert.HasCount(2, frames);
        Assert.AreEqual(2u, frames[0].EventType);
        Assert.AreEqual(3u, frames[1].EventType);
    }

    [TestMethod]
    public void OutboundCapture_FramesProducedBeforeSubscription_AreNotCaptured()
    {
        // The capture only sees frames produced after it is constructed.
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);

        session.Commands.SendEvent(1u); // produced before capture

        using var capture = new OutboundFrameCapture(session);

        session.Commands.SendEvent(2u); // produced after capture

        Assert.HasCount(1, capture.Frames);
        Assert.AreEqual(2u, capture.Frames[0].EventType);
    }

    [TestMethod]
    public void OutboundCapture_Dispose_StopsCapture()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);

        IReadOnlyList<ProtocolFrame> captured;
        using (var capture = new OutboundFrameCapture(session))
        {
            session.Commands.SendEvent(1u);
            captured = capture.Frames;
        }

        // Frames produced after dispose must not appear in the capture.
        session.Commands.SendEvent(2u);

        Assert.HasCount(1, captured);
        Assert.AreEqual(1u, captured[0].EventType);
    }

    // ---------------------------------------------------------------
    // Mixed request + stream outbound frame ordering
    // ---------------------------------------------------------------

    [TestMethod]
    public void FullMixedLifecycle_OutboundFramesInCorrectOrder()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;
        using var capture = new OutboundFrameCapture(session);

        // Local peer sends a request, then opens a session stream, then
        // a handler responds to an inbound request (using ID 100 to avoid
        // colliding with the outgoing request which gets ID 1).
        var outgoing = session.Commands.SendRequest();        // → Request (id=1)
        var stream = session.Commands.OpenSessionStream();    // → StreamOpen

        session.Observer.RequestReceived += (req, _) =>
            req.Respond(payload: new byte[] { 0xAB });                // → Response

        processor.ProcessFrame(ProtocolFrames.Request(100)); // triggers Respond above

        var frames = capture.Frames;
        Assert.HasCount(3, frames);
        Assert.AreEqual(ProtocolFrameKind.Request,    frames[0].Kind);
        Assert.AreEqual(ProtocolFrameKind.StreamOpen, frames[1].Kind);
        Assert.AreEqual(ProtocolFrameKind.Response,   frames[2].Kind);

        // Each frame has the correct correlation ID.
        Assert.AreEqual(outgoing.RequestId, frames[0].RequestId);
        Assert.AreEqual(stream.StreamId,    frames[1].StreamId);
        Assert.AreEqual(100u,               frames[2].RequestId);
    }

    // ---------------------------------------------------------------
    // Cross-subsystem isolation
    // ---------------------------------------------------------------

    [TestMethod]
    public void ClosingRequest_DoesNotAffectConcurrentSessionStream()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var outgoing = session.Commands.SendRequest();
        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));

        processor.ProcessFrame(ProtocolFrames.Response(outgoing.RequestId));

        var snap = session.Diagnostics.GetSnapshot();
        Assert.DoesNotContain(outgoing.RequestId, snap.OpenRequests);
        Assert.Contains(2u, snap.OpenStreams);
    }

    [TestMethod]
    public void ClosingStream_DoesNotAffectConcurrentRequest()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.StreamClose(2));

        var snap = session.Diagnostics.GetSnapshot();
        Assert.Contains(1u, snap.OpenRequests);
        Assert.IsEmpty(snap.OpenStreams);
    }

    [TestMethod]
    public void SendingEvent_DoesNotAffectOpenRequestsOrStreams()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));

        session.Commands.SendEvent(99u);

        var snap = session.Diagnostics.GetSnapshot();
        Assert.Contains(1u, snap.OpenRequests);
        Assert.Contains(2u, snap.OpenStreams);
    }
}
