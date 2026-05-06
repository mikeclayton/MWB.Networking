using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Session.Frames;
using MWB.Networking.Layer2_Protocol.Session.Requests.Api;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

namespace _ProtocolSession;

/// <summary>
/// Tests for inbound request processing: frames arriving from the peer via
/// <see cref="Session.Api.IProtocolSessionProcessor.ProcessFrame"/> and
/// surfaced through <see cref="Session.Api.IProtocolSessionObserver.RequestReceived"/>.
///
/// Also covers the completion of outgoing requests when the peer sends a
/// terminal Response or Error frame.
/// </summary>
[TestClass]
public sealed partial class Requests_Inbound
{
    public TestContext TestContext
    {
        get;
        set;
    }

    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    // ---------------------------------------------------------------
    // Receiving a Request from the peer
    // ---------------------------------------------------------------

    [TestMethod]
    public void InboundRequest_RaisesRequestReceived_ExactlyOnce()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var callCount = 0;
        session.Observer.RequestReceived += (_, _) => callCount++;

        processor.ProcessFrame(ProtocolFrames.Request(1));

        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public void InboundRequest_IncomingRequest_IsNotNull()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingRequest? received = null;
        session.Observer.RequestReceived += (req, _) => received = req;

        processor.ProcessFrame(ProtocolFrames.Request(1));

        Assert.IsNotNull(received);
    }

    [TestMethod]
    public void InboundRequest_IncomingRequest_RequestId_MatchesFrame()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        uint? receivedId = null;
        session.Observer.RequestReceived += (req, _) => receivedId = req.RequestId;

        processor.ProcessFrame(ProtocolFrames.Request(42));

        Assert.AreEqual(42u, receivedId);
    }

    [TestMethod]
    public void InboundRequest_IncomingRequest_RequestType_MatchesFrame()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        uint? receivedType = 0xDEAD;
        session.Observer.RequestReceived += (req, _) => receivedType = req.RequestType;

        processor.ProcessFrame(ProtocolFrames.Request(1, requestType: 99u));

        Assert.AreEqual(99u, receivedType);
    }

    [TestMethod]
    public void InboundRequest_WithNullRequestType_RequestTypeIsNull()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        uint? receivedType = 0xDEAD;
        session.Observer.RequestReceived += (req, _) => receivedType = req.RequestType;

        processor.ProcessFrame(ProtocolFrames.Request(1, requestType: null));

        Assert.IsNull(receivedType);
    }

    [TestMethod]
    public void InboundRequest_Payload_MatchesFrame()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        ReadOnlyMemory<byte> receivedPayload = default;
        session.Observer.RequestReceived += (_, payload) => receivedPayload = payload;

        var sent = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        processor.ProcessFrame(ProtocolFrames.Request(1, payload: sent));

        CollectionAssert.AreEqual(sent, receivedPayload.ToArray());
    }

    [TestMethod]
    public void InboundRequest_WithEmptyPayload_HandlerReceivesEmptyPayload()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var receivedLength = -1;
        session.Observer.RequestReceived += (_, payload) => receivedLength = payload.Length;

        processor.ProcessFrame(ProtocolFrames.Request(1));

        Assert.AreEqual(0, receivedLength);
    }

    [TestMethod]
    public void InboundRequest_DoesNotEmitOutboundFrames()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;
        using var capture = new OutboundFrameCapture(session);

        session.Observer.RequestReceived += (_, _) => { };
        processor.ProcessFrame(ProtocolFrames.Request(1));

        Assert.IsEmpty(capture.Frames);
    }

    [TestMethod]
    public void InboundRequest_WithNoHandlers_DoesNotThrow()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        // No handler registered — should not throw.
        processor.ProcessFrame(ProtocolFrames.Request(1));
    }

    // ---------------------------------------------------------------
    // Snapshot state
    // ---------------------------------------------------------------

    [TestMethod]
    public void InboundRequest_AppearsInSnapshot()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));

        Assert.Contains(1u, session.Diagnostics.GetSnapshot().OpenRequests);
    }

    [TestMethod]
    public void MultipleConcurrentRequests_AllTrackedInSnapshot()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(10));
        processor.ProcessFrame(ProtocolFrames.Request(20));
        processor.ProcessFrame(ProtocolFrames.Request(30));

        var snap = session.Diagnostics.GetSnapshot();

        Assert.HasCount(3, snap.OpenRequests);
        Assert.Contains(10u, snap.OpenRequests);
        Assert.Contains(20u, snap.OpenRequests);
        Assert.Contains(30u, snap.OpenRequests);
    }

    // ---------------------------------------------------------------
    // Inbound Response / Error closes the corresponding outgoing request
    // ---------------------------------------------------------------

    [TestMethod]
    public void InboundResponse_ClosesOutgoingRequest()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var outgoing = session.Commands.SendRequest();
        processor.ProcessFrame(ProtocolFrames.Response(outgoing.RequestId));

        Assert.DoesNotContain(outgoing.RequestId, session.Diagnostics.GetSnapshot().OpenRequests);
    }

    [TestMethod]
    public void InboundError_ClosesOutgoingRequest()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var outgoing = session.Commands.SendRequest();
        processor.ProcessFrame(ProtocolFrames.Error(outgoing.RequestId));

        Assert.DoesNotContain(outgoing.RequestId, session.Diagnostics.GetSnapshot().OpenRequests);
    }

    [TestMethod]
    public void InboundResponse_CompletesResponseTask()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var outgoing = session.Commands.SendRequest();

        Assert.IsFalse(outgoing.Response.IsCompleted,
            "ResponseTask must not be complete before a response is received.");

        processor.ProcessFrame(ProtocolFrames.Response(outgoing.RequestId));

        Assert.IsTrue(outgoing.Response.IsCompletedSuccessfully,
            "ResponseTask must complete when a Response frame is received.");
    }

    [TestMethod]
    public void InboundError_CompletesResponseTask()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var outgoing = session.Commands.SendRequest();

        Assert.IsFalse(outgoing.Response.IsCompleted);

        processor.ProcessFrame(ProtocolFrames.Error(outgoing.RequestId));

        Assert.IsTrue(outgoing.Response.IsCompletedSuccessfully,
            "ResponseTask must complete when an Error frame is received.");
    }

    [TestMethod]
    public void InboundResponse_ResponseTask_CarriesCorrectPayload()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var outgoing = session.Commands.SendRequest();
        var responsePayload = new byte[] { 0xCA, 0xFE };

        processor.ProcessFrame(ProtocolFrames.Response(outgoing.RequestId, payload: responsePayload));

        var responseFrame = outgoing.Response.Result;
        Assert.AreEqual(ProtocolFrameKind.Response, responseFrame.Kind);
        CollectionAssert.AreEqual(responsePayload, responseFrame.Payload.ToArray());
    }

    [TestMethod]
    public void InboundResponse_DoesNotEmitOutboundFrames()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var outgoing = session.Commands.SendRequest();
        using var capture = new OutboundFrameCapture(session);
        capture.Drain(); // clear the SendRequest frame

        processor.ProcessFrame(ProtocolFrames.Response(outgoing.RequestId));

        Assert.IsEmpty(capture.Frames);
    }

    [TestMethod]
    public void InboundError_DoesNotEmitOutboundFrames()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var outgoing = session.Commands.SendRequest();
        using var capture = new OutboundFrameCapture(session);
        capture.Drain(); // clear the SendRequest frame

        processor.ProcessFrame(ProtocolFrames.Error(outgoing.RequestId));

        Assert.IsEmpty(capture.Frames);
    }

    // ---------------------------------------------------------------
    // Multiple requests close independently
    // ---------------------------------------------------------------

    [TestMethod]
    public void MultipleRequests_CloseIndependently()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var req1 = session.Commands.SendRequest();
        var req2 = session.Commands.SendRequest();

        processor.ProcessFrame(ProtocolFrames.Response(req1.RequestId));

        var snap = session.Diagnostics.GetSnapshot();

        Assert.DoesNotContain(req1.RequestId, snap.OpenRequests);
        Assert.Contains(req2.RequestId, snap.OpenRequests);
    }

    [TestMethod]
    public void RequestId_ReusableAfterClose()
    {
        // Sending a Request then receiving a Response should free the ID for reuse
        // as an inbound request from the peer on the same transport.
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.ProcessFrame(ProtocolFrames.Response(1));

        // Peer uses the same ID for a new request — must be accepted.
        processor.ProcessFrame(ProtocolFrames.Request(1));

        Assert.Contains(1u, session.Diagnostics.GetSnapshot().OpenRequests);
    }
}
