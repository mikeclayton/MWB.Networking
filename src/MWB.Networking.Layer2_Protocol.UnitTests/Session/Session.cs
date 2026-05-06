using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Session.Frames;
using MWB.Networking.Layer2_Protocol.Session.Requests.Api;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

namespace _ProtocolSession;

/// <summary>
/// Cross-cutting tests: DrainOutbound semantics, Snapshot accuracy,
/// null/unknown-kind guards, and mixed request + stream scenarios.
/// </summary>
[TestClass]
public sealed partial class Session
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
    // DrainOutbound semantics
    // ---------------------------------------------------------------

    [TestMethod]
    public void DrainOutbound_IsEmptyAtStart()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        var outbound = processor.DrainOutboundFrames();

        Assert.IsEmpty(outbound);
    }

    [TestMethod]
    public void DrainOutbound_ClearsQueueAfterFirstCall()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        IncomingRequest? r1 = null;
        IncomingRequest? r2 = null;

        session.Observer.RequestReceived += (req, payload) =>
        {
            if (req.Context.RequestId == 1) r1 = req;
            if (req.Context.RequestId == 2) r2 = req;
        };

        // First outbound frame
        processor.ProcessFrame(ProtocolFrames.Request(1));
        r1!.Respond(new byte[] { 0x01 });

        var first = processor.DrainOutboundFrames();
        var second = processor.DrainOutboundFrames();

        Assert.HasCount(1, first);
        Assert.IsEmpty(second);

        // Prove new outbound frames appear after drain
        processor.ProcessFrame(ProtocolFrames.Request(2));
        r2!.Respond(new byte[] { 0x02 });

        var third = processor.DrainOutboundFrames();

        Assert.HasCount(1, third);
        Assert.AreEqual((byte)0x02, third[0].Payload.Span[0]);
    }


    [TestMethod]

    public void DrainOutbound_AccumulatesAcrossMultipleOutboundEmits()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        IncomingRequest? request1 = null;
        IncomingRequest? request2 = null;

        session.Observer.RequestReceived += (req, payload) =>
        {
            if (req.Context.RequestId == 1)
            {
                request1 = req;
            }
            else if (req.Context.RequestId == 2)
            {
                request2 = req;
            }
        };

        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.ProcessFrame(ProtocolFrames.Request(2));

        // Each request produces exactly one response
        request1!.Respond(new byte[] { 0xA1 });
        request2!.Respond(new byte[] { 0xA2 });

        var outbound = processor.DrainOutboundFrames();

        Assert.HasCount(2, outbound);
        Assert.AreEqual(ProtocolFrameKind.Response, outbound[0].Kind);
        Assert.AreEqual(ProtocolFrameKind.Response, outbound[1].Kind);
    }


    [TestMethod]
    public void DrainOutbound_AfterPartialDrain_ContainsOnlyNewOutboundFrames()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        IncomingRequest? r1 = null;
        IncomingRequest? r2 = null;
        IncomingRequest? r3 = null;

        session.Observer.RequestReceived += (req, payload) =>
        {
            if (req.Context.RequestId == 1) r1 = req;
            if (req.Context.RequestId == 2) r2 = req;
            if (req.Context.RequestId == 3) r3 = req;
        };

        // First outbound frame
        processor.ProcessFrame(ProtocolFrames.Request(1));
        r1!.Respond(new byte[] { 1 });

        // Drain existing outbound frames
        processor.DrainOutboundFrames();

        // Two new outbound frames
        processor.ProcessFrame(ProtocolFrames.Request(2));
        processor.ProcessFrame(ProtocolFrames.Request(3));
        r2!.Respond(new byte[] { 2 });
        r3!.Respond(new byte[] { 3 });

        var outbound = processor.DrainOutboundFrames();

        Assert.HasCount(2, outbound);
        Assert.AreEqual((byte)2, outbound[0].Payload.Span[0]);
        Assert.AreEqual((byte)3, outbound[1].Payload.Span[0]);
    }

    // ---------------------------------------------------------------
    // Snapshot accuracy
    // ---------------------------------------------------------------

    [TestMethod]
    public void Snapshot_InitiallyEmpty()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);

        var snap = session.Diagnostics.GetSnapshot();

        Assert.IsEmpty(snap.OpenRequests);
        Assert.IsEmpty(snap.OpenStreams);
    }

    [TestMethod]
    public void Snapshot_IsNonDestructive()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(10));

        var snap1 = session.Diagnostics.GetSnapshot();
        var snap2 = session.Diagnostics.GetSnapshot();

        Assert.HasCount(snap1.OpenRequests.Count, snap2.OpenRequests);
        Assert.HasCount(snap1.OpenStreams.Count, snap2.OpenStreams);
    }

    [TestMethod]
    public void Snapshot_AllClosed_ShowsEmpty()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(10));
        processor.ProcessFrame(ProtocolFrames.Response(1));
        processor.ProcessFrame(ProtocolFrames.StreamClose(10));

        var snap = session.Diagnostics.GetSnapshot();

        Assert.IsEmpty(snap.OpenRequests);
        Assert.IsEmpty(snap.OpenStreams);
    }

    [TestMethod]
    public void Snapshot_RequestsAndStreams_AreIndependentIdNamespaces()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        // The same numeric ID may be in use simultaneously as both a request and a stream.
        processor.ProcessFrame(ProtocolFrames.Request(42));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(42));

        var snap = session.Diagnostics.GetSnapshot();

        Assert.Contains(42u, snap.OpenRequests);
        Assert.Contains(42u, snap.OpenStreams);
    }

    [TestMethod]
    public void Snapshot_DoesNotContainStreamIdsInRequestsOrViceVersa()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));

        var snap = session.Diagnostics.GetSnapshot();

        Assert.DoesNotContain(2u, snap.OpenRequests);
        Assert.DoesNotContain(1u, snap.OpenStreams);
    }

    // ---------------------------------------------------------------
    // Null / unknown-kind guards
    // ---------------------------------------------------------------

    [TestMethod]
    public void OnInbound_NullFrame_ThrowsArgumentNullException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        Assert.Throws<ArgumentNullException>(() => processor.ProcessFrame(null!));
    }


    [TestMethod]
    public void OnInbound_UnknownFrameKind_ThrowsProtocolException_WithUnknownFrameKindError()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        // Arrange: intentionally invalid protocol frame
        var frame = ProtocolFrameGenerator.CreateInvalidProtocolFrame(
            (ProtocolFrameKind)0xFF);

        // Act
        var ex = Assert.Throws<ProtocolException>(
            () => processor.ProcessFrame(frame));

        // Assert
        Assert.AreEqual(
            ProtocolErrorKind.UnknownFrameKind,
            ex.ErrorKind);
    }

    // ---------------------------------------------------------------
    // Mixed request + stream scenarios
    // ---------------------------------------------------------------

    [TestMethod]
    public void MixedRequestsAndStreams_TrackedIndependently()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.ProcessFrame(ProtocolFrames.Request(2));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(10));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(20));

        var snap = session.Diagnostics.GetSnapshot();

        Assert.HasCount(2, snap.OpenRequests);
        Assert.HasCount(2, snap.OpenStreams);
    }

    [TestMethod]
    public void ClosingRequest_DoesNotAffectOpenStreams()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(10));
        processor.ProcessFrame(ProtocolFrames.Response(1));

        var snap = session.Diagnostics.GetSnapshot();

        Assert.IsEmpty(snap.OpenRequests);
        Assert.HasCount(1, snap.OpenStreams);
        Assert.Contains(10u, snap.OpenStreams);
    }

    [TestMethod]
    public void ClosingStream_DoesNotAffectOpenRequests()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(10));
        processor.ProcessFrame(ProtocolFrames.StreamClose(10));

        var snap = session.Diagnostics.GetSnapshot();

        Assert.HasCount(1, snap.OpenRequests);
        Assert.Contains(1u, snap.OpenRequests);
        Assert.IsEmpty(snap.OpenStreams);
    }

    [TestMethod]
    public void Inbound_ResponseFrame_IsAccepted()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.DrainOutboundFrames();

        // Should not throw
        processor.ProcessFrame(ProtocolFrames.Response(1, null, new byte[] { 0x01 }));

        var outbound = processor.DrainOutboundFrames();
        Assert.HasCount(0, outbound);
    }


    [TestMethod]
    public void FullMixedLifecycle_SnapshotAccurateAtEachStep()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        // Open two requests and two streams.
        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.ProcessFrame(ProtocolFrames.Request(2));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(10));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(20));
        Assert.HasCount(2, session.Diagnostics.GetSnapshot().OpenRequests);
        Assert.HasCount(2, session.Diagnostics.GetSnapshot().OpenStreams);

        // Close one request and one stream.
        processor.ProcessFrame(ProtocolFrames.Response(1));
        processor.ProcessFrame(ProtocolFrames.StreamClose(10));
        Assert.HasCount(1, session.Diagnostics.GetSnapshot().OpenRequests);
        Assert.HasCount(1, session.Diagnostics.GetSnapshot().OpenStreams);

        // Close the remaining ones.
        processor.ProcessFrame(ProtocolFrames.Error(2));
        processor.ProcessFrame(ProtocolFrames.StreamClose(20));
        Assert.IsEmpty(session.Diagnostics.GetSnapshot().OpenRequests);
        Assert.IsEmpty(session.Diagnostics.GetSnapshot().OpenStreams);
    }
}
