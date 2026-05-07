using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Session.Frames;
using MWB.Networking.Layer2_Protocol.Session.Requests.Api;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

namespace _ProtocolSession;

/// <summary>
/// Tests for outbound request sending via
/// <see cref="Session.Api.IProtocolSessionCommands.SendRequest"/> and
/// for responding to inbound requests via <see cref="IncomingRequest.Respond"/>
/// and <see cref="IncomingRequest.Error"/>.
/// </summary>
[TestClass]
public sealed partial class Requests_Outbound
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
    // SendRequest — frame structure
    // ---------------------------------------------------------------

    [TestMethod]
    public void SendRequest_EmitsSingleFrame()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.SendRequest();

        Assert.HasCount(1, capture.Frames);
    }

    [TestMethod]
    public void SendRequest_EmittedFrame_HasKindRequest()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.SendRequest();

        Assert.AreEqual(ProtocolFrameKind.Request, capture.Frames[0].Kind);
    }

    [TestMethod]
    public void SendRequest_EmittedFrame_HasRequestId()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.SendRequest();

        Assert.IsNotNull(capture.Frames[0].RequestId);
    }

    [TestMethod]
    public void SendRequest_EmittedFrame_RequestId_MatchesReturnedHandle()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        var outgoing = session.Commands.SendRequest();

        Assert.AreEqual(outgoing.RequestId, capture.Frames[0].RequestId);
    }

    [TestMethod]
    public void SendRequest_EmittedFrame_CarriesCorrectPayload()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);
        var payload = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        session.Commands.SendRequest(payload: payload);

        CollectionAssert.AreEqual(payload, capture.Frames[0].Payload.ToArray());
    }

    [TestMethod]
    public void SendRequest_EmittedFrame_CarriesCorrectRequestType()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.SendRequest(requestType: 77u);

        Assert.AreEqual(77u, capture.Frames[0].RequestType);
    }

    [TestMethod]
    public void SendRequest_WithNullRequestType_EmittedFrame_HasNullRequestType()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.SendRequest(requestType: null);

        Assert.IsNull(capture.Frames[0].RequestType);
    }

    [TestMethod]
    public void SendRequest_EmittedFrame_HasNullStreamId()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.SendRequest();

        Assert.IsNull(capture.Frames[0].StreamId);
    }

    // ---------------------------------------------------------------
    // SendRequest — snapshot and handle
    // ---------------------------------------------------------------

    [TestMethod]
    public void SendRequest_AppearsInSnapshot()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);

        var outgoing = session.Commands.SendRequest();

        Assert.Contains(outgoing.RequestId, session.Diagnostics.GetSnapshot().OpenRequests);
    }

    [TestMethod]
    public void SendRequest_DoesNotFireInboundRequestReceivedHandler()
    {
        // Outbound requests must not loop back to the local observer.
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);

        var callCount = 0;
        session.Observer.RequestReceived += (_, _) => callCount++;

        session.Commands.SendRequest();

        Assert.AreEqual(0, callCount);
    }

    [TestMethod]
    public void MultipleSendRequests_HaveDistinctIds()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);

        var r1 = session.Commands.SendRequest();
        var r2 = session.Commands.SendRequest();
        var r3 = session.Commands.SendRequest();

        Assert.AreNotEqual(r1.RequestId, r2.RequestId);
        Assert.AreNotEqual(r2.RequestId, r3.RequestId);
        Assert.AreNotEqual(r1.RequestId, r3.RequestId);
    }

    // ---------------------------------------------------------------
    // Responding to an inbound request — Respond()
    // ---------------------------------------------------------------

    [TestMethod]
    public void Respond_EmitsResponseFrame()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingRequest? request = null;
        session.Observer.RequestReceived += (req, _) => request = req;
        processor.ProcessFrame(ProtocolFrames.Request(1));

        using var capture = new OutboundFrameCapture(session);

        Assert.IsNotNull(request);
        request.Respond(payload: new byte[] { 0xAB, 0xCD });

        Assert.HasCount(1, capture.Frames);
        Assert.AreEqual(ProtocolFrameKind.Response, capture.Frames[0].Kind);
    }

    [TestMethod]
    public void Respond_EmittedFrame_HasCorrectRequestId()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingRequest? request = null;
        session.Observer.RequestReceived += (req, _) => request = req;
        processor.ProcessFrame(ProtocolFrames.Request(1));

        using var capture = new OutboundFrameCapture(session);
        request!.Respond(payload: new byte[] { 0x01 });

        Assert.AreEqual(1u, capture.Frames[0].RequestId);
    }

    [TestMethod]
    public void Respond_EmittedFrame_CarriesCorrectPayload()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingRequest? request = null;
        session.Observer.RequestReceived += (req, _) => request = req;
        processor.ProcessFrame(ProtocolFrames.Request(1));

        using var capture = new OutboundFrameCapture(session);
        var payload = new byte[] { 0xAB, 0xCD };
        request!.Respond(payload: payload);

        CollectionAssert.AreEqual(payload, capture.Frames[0].Payload.ToArray());
    }

    [TestMethod]
    public void Respond_ClosesRequest()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingRequest? request = null;
        session.Observer.RequestReceived += (req, _) => request = req;
        processor.ProcessFrame(ProtocolFrames.Request(1));

        request!.Respond();

        Assert.DoesNotContain(1u, session.Diagnostics.GetSnapshot().OpenRequests);
    }

    [TestMethod]
    public void Respond_CalledTwice_ThrowsInvalidOperationException()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingRequest? request = null;
        session.Observer.RequestReceived += (req, _) => request = req;
        processor.ProcessFrame(ProtocolFrames.Request(1));

        request!.Respond(payload: new byte[] { 0xA1 });

        Assert.Throws<InvalidOperationException>(() => request.Respond(payload: new byte[] { 0xA2 }));
    }

    [TestMethod]
    public void Respond_CalledTwice_SecondCallEmitsNoFrame()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingRequest? request = null;
        session.Observer.RequestReceived += (req, _) => request = req;
        processor.ProcessFrame(ProtocolFrames.Request(1));

        request!.Respond(payload: new byte[] { 0xA1 });

        using var capture = new OutboundFrameCapture(session);

        try { request.Respond(payload: new byte[] { 0xA2 }); } catch (InvalidOperationException) { }

        Assert.IsEmpty(capture.Frames);
    }

    // ---------------------------------------------------------------
    // Responding to an inbound request — Reject()
    // ---------------------------------------------------------------

    [TestMethod]
    public void Reject_EmitsErrorFrame()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingRequest? request = null;
        session.Observer.RequestReceived += (req, _) => request = req;
        processor.ProcessFrame(ProtocolFrames.Request(1));

        using var capture = new OutboundFrameCapture(session);
        request!.Reject(new byte[] { 0xFF });

        Assert.HasCount(1, capture.Frames);
        Assert.AreEqual(ProtocolFrameKind.Error, capture.Frames[0].Kind);
    }

    [TestMethod]
    public void Reject_EmittedFrame_HasCorrectRequestId()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingRequest? request = null;
        session.Observer.RequestReceived += (req, _) => request = req;
        processor.ProcessFrame(ProtocolFrames.Request(1));

        using var capture = new OutboundFrameCapture(session);
        request!.Reject();

        Assert.AreEqual(1u, capture.Frames[0].RequestId);
    }

    [TestMethod]
    public void Reject_EmittedFrame_CarriesCorrectPayload()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingRequest? request = null;
        session.Observer.RequestReceived += (req, _) => request = req;
        processor.ProcessFrame(ProtocolFrames.Request(1));

        using var capture = new OutboundFrameCapture(session);
        var payload = new byte[] { 0xDE, 0xAD };
        request!.Reject(payload);

        CollectionAssert.AreEqual(payload, capture.Frames[0].Payload.ToArray());
    }

    [TestMethod]
    public void Reject_ClosesRequest()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingRequest? request = null;
        session.Observer.RequestReceived += (req, _) => request = req;
        processor.ProcessFrame(ProtocolFrames.Request(1));

        request!.Reject();

        Assert.DoesNotContain(1u, session.Diagnostics.GetSnapshot().OpenRequests);
    }

    [TestMethod]
    public void Reject_CalledTwice_ThrowsInvalidOperationException()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingRequest? request = null;
        session.Observer.RequestReceived += (req, _) => request = req;
        processor.ProcessFrame(ProtocolFrames.Request(1));

        request!.Reject();

        Assert.Throws<InvalidOperationException>(() => request.Reject());
    }

    [TestMethod]
    public void Reject_AfterRespond_ThrowsInvalidOperationException()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingRequest? request = null;
        session.Observer.RequestReceived += (req, _) => request = req;
        processor.ProcessFrame(ProtocolFrames.Request(1));

        request!.Respond();

        Assert.Throws<InvalidOperationException>(() => request.Reject());
    }

    // ---------------------------------------------------------------
    // Multiple concurrent requests
    // ---------------------------------------------------------------

    [TestMethod]
    public void TwoConcurrentRequests_ResponsesEmittedInOrder()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var requests = new List<IncomingRequest>();
        session.Observer.RequestReceived += (req, _) => requests.Add(req);

        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.ProcessFrame(ProtocolFrames.Request(2));

        using var capture = new OutboundFrameCapture(session);

        requests[0].Respond(payload: new byte[] { 0x11 });
        requests[1].Respond(payload: new byte[] { 0x22 });

        Assert.HasCount(2, capture.Frames);
        Assert.AreEqual(1u, capture.Frames[0].RequestId);
        Assert.AreEqual(2u, capture.Frames[1].RequestId);
    }
}
