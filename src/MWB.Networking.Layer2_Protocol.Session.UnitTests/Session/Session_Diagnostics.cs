using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Session.Frames;
using MWB.Networking.Layer2_Protocol.Session.UnitTests.Helpers;

namespace _ProtocolSession;

/// <summary>
/// Tests for <see cref="Session.Api.IProtocolSessionDiagnostics.GetSnapshot"/>:
/// the snapshot must accurately reflect the set of open requests and streams at
/// every point in the session lifecycle, and must be non-destructive.
/// </summary>
[TestClass]
public sealed class Session_Diagnostics
{
    public TestContext TestContext { get; set; }

    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    // ---------------------------------------------------------------
    // Initial state
    // ---------------------------------------------------------------

    [TestMethod]
    public void Snapshot_InitiallyEmpty()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);

        var snap = session.Diagnostics.GetSnapshot();

        Assert.IsEmpty(snap.OpenRequests);
        Assert.IsEmpty(snap.OpenStreams);
    }

    // ---------------------------------------------------------------
    // Non-destructive reads
    // ---------------------------------------------------------------

    [TestMethod]
    public void Snapshot_IsNonDestructive()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));

        var snap1 = session.Diagnostics.GetSnapshot();
        var snap2 = session.Diagnostics.GetSnapshot();

        Assert.HasCount(snap1.OpenRequests.Count, snap2.OpenRequests);
        Assert.HasCount(snap1.OpenStreams.Count, snap2.OpenStreams);
    }

    // ---------------------------------------------------------------
    // Request tracking
    // ---------------------------------------------------------------

    [TestMethod]
    public void Snapshot_ContainsRequestAfterOpen()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));

        Assert.Contains(1u, session.Diagnostics.GetSnapshot().OpenRequests);
    }

    [TestMethod]
    public void Snapshot_DoesNotContainRequestAfterResponseReceived()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var outgoing = session.Commands.SendRequest();
        processor.ProcessFrame(ProtocolFrames.Response(outgoing.RequestId));

        Assert.DoesNotContain(outgoing.RequestId, session.Diagnostics.GetSnapshot().OpenRequests);
    }

    [TestMethod]
    public void Snapshot_DoesNotContainRequestAfterErrorReceived()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var outgoing = session.Commands.SendRequest();
        processor.ProcessFrame(ProtocolFrames.Error(outgoing.RequestId));

        Assert.DoesNotContain(outgoing.RequestId, session.Diagnostics.GetSnapshot().OpenRequests);
    }

    [TestMethod]
    public void Snapshot_DoesNotContainInboundRequestAfterRespond()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        session.Observer.RequestReceived += (req, _) => req.Respond();
        processor.ProcessFrame(ProtocolFrames.Request(1));

        Assert.DoesNotContain(1u, session.Diagnostics.GetSnapshot().OpenRequests);
    }

    // ---------------------------------------------------------------
    // Stream tracking
    // ---------------------------------------------------------------

    [TestMethod]
    public void Snapshot_ContainsStreamAfterOpen()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));

        Assert.Contains(2u, session.Diagnostics.GetSnapshot().OpenStreams);
    }

    [TestMethod]
    public void Snapshot_DoesNotContainStreamAfterClose()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.StreamClose(2));

        Assert.DoesNotContain(2u, session.Diagnostics.GetSnapshot().OpenStreams);
    }

    [TestMethod]
    public void Snapshot_ContainsOutgoingStreamAfterOpen()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);

        var stream = session.Commands.OpenSessionStream();

        Assert.Contains(stream.StreamId, session.Diagnostics.GetSnapshot().OpenStreams);
    }

    [TestMethod]
    public void Snapshot_DoesNotContainOutgoingStreamAfterClose()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);

        var stream = session.Commands.OpenSessionStream();
        stream.Close();

        Assert.DoesNotContain(stream.StreamId, session.Diagnostics.GetSnapshot().OpenStreams);
    }

    // ---------------------------------------------------------------
    // Namespace independence
    // ---------------------------------------------------------------

    [TestMethod]
    public void Snapshot_RequestsAndStreams_AreIndependentIdNamespaces()
    {
        // The same numeric ID may be in use simultaneously as both a request and a stream.
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(42));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(42));

        var snap = session.Diagnostics.GetSnapshot();

        Assert.Contains(42u, snap.OpenRequests);
        Assert.Contains(42u, snap.OpenStreams);
    }

    [TestMethod]
    public void Snapshot_StreamIdsNotVisibleInRequests_AndViceVersa()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));

        var snap = session.Diagnostics.GetSnapshot();

        Assert.DoesNotContain(2u, snap.OpenRequests);
        Assert.DoesNotContain(1u, snap.OpenStreams);
    }

    // ---------------------------------------------------------------
    // Full lifecycle progression
    // ---------------------------------------------------------------

    [TestMethod]
    public void Snapshot_AllClosed_ShowsEmpty()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var outgoing = session.Commands.SendRequest();
        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.Response(outgoing.RequestId));
        processor.ProcessFrame(ProtocolFrames.StreamClose(2));

        var snap = session.Diagnostics.GetSnapshot();

        Assert.IsEmpty(snap.OpenRequests);
        Assert.IsEmpty(snap.OpenStreams);
    }

    [TestMethod]
    public void Snapshot_AccurateAtEachStepOfMixedLifecycle()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        // Use outgoing requests so Response/Error frames are valid.
        // Use even stream IDs so inbound StreamOpen frames pass parity validation.
        var req1 = session.Commands.SendRequest();
        var req2 = session.Commands.SendRequest();
        processor.ProcessFrame(ProtocolFrames.StreamOpen(10));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(20));

        Assert.HasCount(2, session.Diagnostics.GetSnapshot().OpenRequests);
        Assert.HasCount(2, session.Diagnostics.GetSnapshot().OpenStreams);

        processor.ProcessFrame(ProtocolFrames.Response(req1.RequestId));
        processor.ProcessFrame(ProtocolFrames.StreamClose(10));

        Assert.HasCount(1, session.Diagnostics.GetSnapshot().OpenRequests);
        Assert.HasCount(1, session.Diagnostics.GetSnapshot().OpenStreams);

        processor.ProcessFrame(ProtocolFrames.Error(req2.RequestId));
        processor.ProcessFrame(ProtocolFrames.StreamClose(20));

        Assert.IsEmpty(session.Diagnostics.GetSnapshot().OpenRequests);
        Assert.IsEmpty(session.Diagnostics.GetSnapshot().OpenStreams);
    }
}
