using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Session.Frames;
using MWB.Networking.Layer2_Protocol.Session.Requests.Api;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

namespace _ProtocolSession;

/// <summary>
/// Tests for request-scoped outgoing streams opened via
/// <see cref="IncomingRequest.OpenRequestStream"/>.
/// A request-scoped stream must be opened before the request is responded to,
/// and at most one may exist per request.
/// </summary>
[TestClass]
public sealed partial class Streams_RequestScoped
{
    public TestContext TestContext { get; set; }

    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    // ---------------------------------------------------------------
    // StreamOpen — frame structure
    // ---------------------------------------------------------------

    [TestMethod]
    public void OpenRequestStream_EmitsStreamOpenFrame()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingRequest? request = null;
        session.Observer.RequestReceived += (req, _) => request = req;
        processor.ProcessFrame(ProtocolFrames.Request(1));

        using var capture = new OutboundFrameCapture(session);
        request!.OpenRequestStream(null);

        Assert.HasCount(1, capture.Frames);
        Assert.AreEqual(ProtocolFrameKind.StreamOpen, capture.Frames[0].Kind);
    }

    [TestMethod]
    public void OpenRequestStream_EmittedFrame_CarriesRequestId()
    {
        // A request-scoped StreamOpen must carry the owning RequestId.
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingRequest? request = null;
        session.Observer.RequestReceived += (req, _) => request = req;
        processor.ProcessFrame(ProtocolFrames.Request(1));

        using var capture = new OutboundFrameCapture(session);
        request!.OpenRequestStream(null);

        Assert.AreEqual(1u, capture.Frames[0].RequestId);
    }

    [TestMethod]
    public void OpenRequestStream_EmittedFrame_HasStreamId()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingRequest? request = null;
        session.Observer.RequestReceived += (req, _) => request = req;
        processor.ProcessFrame(ProtocolFrames.Request(1));

        using var capture = new OutboundFrameCapture(session);
        request!.OpenRequestStream(null);

        Assert.IsNotNull(capture.Frames[0].StreamId);
    }

    [TestMethod]
    public void OpenRequestStream_EmittedFrame_CarriesStreamType()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingRequest? request = null;
        session.Observer.RequestReceived += (req, _) => request = req;
        processor.ProcessFrame(ProtocolFrames.Request(1));

        using var capture = new OutboundFrameCapture(session);
        request!.OpenRequestStream(streamType: 33u);

        Assert.AreEqual(33u, capture.Frames[0].StreamType);
    }

    [TestMethod]
    public void OpenRequestStream_AppearsInSnapshot()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingRequest? request = null;
        session.Observer.RequestReceived += (req, _) => request = req;
        processor.ProcessFrame(ProtocolFrames.Request(1));

        var stream = request!.OpenRequestStream(null);

        Assert.Contains(stream.StreamId, session.Diagnostics.GetSnapshot().OpenStreams);
    }

    // ---------------------------------------------------------------
    // Full lifecycle
    // ---------------------------------------------------------------

    [TestMethod]
    public void FullRequestScopedStreamLifecycle_AllFramesEmittedInOrder()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingRequest? request = null;
        session.Observer.RequestReceived += (req, _) => request = req;
        processor.ProcessFrame(ProtocolFrames.Request(1));

        using var capture = new OutboundFrameCapture(session);

        var stream = request!.OpenRequestStream(null);
        stream.SendData(new byte[] { 0xA1 });
        stream.SendData(new byte[] { 0xA2 });
        stream.Close();

        var frames = capture.Frames;
        Assert.HasCount(4, frames);
        Assert.AreEqual(ProtocolFrameKind.StreamOpen,  frames[0].Kind);
        Assert.AreEqual(ProtocolFrameKind.StreamData,  frames[1].Kind);
        Assert.AreEqual(ProtocolFrameKind.StreamData,  frames[2].Kind);
        Assert.AreEqual(ProtocolFrameKind.StreamClose, frames[3].Kind);
    }

    // ---------------------------------------------------------------
    // Request-scoped stream closed when request is responded to
    // ---------------------------------------------------------------

    [TestMethod]
    public void RequestScopedStream_RemovedFromSnapshot_WhenRequestIsResponded()
    {
        // When the owning request is closed, its request-scoped stream must also
        // be torn down and disappear from the open-streams snapshot.
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingRequest? request = null;
        session.Observer.RequestReceived += (req, _) => request = req;
        processor.ProcessFrame(ProtocolFrames.Request(1));

        var stream = request!.OpenRequestStream(null);
        Assert.Contains(stream.StreamId, session.Diagnostics.GetSnapshot().OpenStreams);

        request.Respond();

        Assert.DoesNotContain(stream.StreamId, session.Diagnostics.GetSnapshot().OpenStreams);
    }

    [TestMethod]
    public void RequestScopedStream_RemovedFromSnapshot_WhenRequestReceivesInboundResponse()
    {
        // When the peer closes our outgoing request with a Response frame,
        // any request-scoped stream we opened must also be torn down.
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var outgoing = session.Commands.SendRequest();
        var stream = outgoing.OpenRequestStream(null);

        Assert.Contains(stream.StreamId, session.Diagnostics.GetSnapshot().OpenStreams);

        processor.ProcessFrame(ProtocolFrames.Response(outgoing.RequestId));

        Assert.DoesNotContain(stream.StreamId, session.Diagnostics.GetSnapshot().OpenStreams);
    }

    // ---------------------------------------------------------------
    // Lifecycle guards
    // ---------------------------------------------------------------

    [TestMethod]
    public void OpenRequestStream_AfterRespond_ThrowsInvalidOperationException()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingRequest? request = null;
        session.Observer.RequestReceived += (req, _) => request = req;
        processor.ProcessFrame(ProtocolFrames.Request(1));
        request!.Respond();

        Assert.Throws<InvalidOperationException>(() => request.OpenRequestStream(null));
    }

    [TestMethod]
    public void OpenRequestStream_Twice_ThrowsInvalidOperationException()
    {
        // At most one request-scoped stream may be opened per request.
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingRequest? request = null;
        session.Observer.RequestReceived += (req, _) => request = req;
        processor.ProcessFrame(ProtocolFrames.Request(1));
        request!.OpenRequestStream(null);

        Assert.Throws<InvalidOperationException>(() => request.OpenRequestStream(null));
    }
}
