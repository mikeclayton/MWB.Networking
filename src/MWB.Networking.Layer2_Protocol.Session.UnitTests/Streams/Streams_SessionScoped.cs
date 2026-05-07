using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Session.Frames;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

namespace _ProtocolSession;

/// <summary>
/// Tests for session-scoped outgoing streams opened via
/// <see cref="Session.Api.IProtocolSessionCommands.OpenSessionStream"/>.
/// Session-scoped streams have no owning request and may be opened at any time.
/// </summary>
[TestClass]
public sealed partial class Streams_SessionScoped
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
    public void OpenSessionStream_EmitsStreamOpenFrame()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.OpenSessionStream();

        Assert.HasCount(1, capture.Frames);
        Assert.AreEqual(ProtocolFrameKind.StreamOpen, capture.Frames[0].Kind);
    }

    [TestMethod]
    public void OpenSessionStream_EmittedFrame_HasStreamId()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.OpenSessionStream();

        Assert.IsNotNull(capture.Frames[0].StreamId);
    }

    [TestMethod]
    public void OpenSessionStream_EmittedFrame_StreamId_MatchesReturnedHandle()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        var stream = session.Commands.OpenSessionStream();

        Assert.AreEqual(stream.StreamId, capture.Frames[0].StreamId);
    }

    [TestMethod]
    public void OpenSessionStream_EmittedFrame_CarriesMetadata()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);
        var metadata = new byte[] { 0x01, 0x02 };

        session.Commands.OpenSessionStream(metadata: metadata);

        CollectionAssert.AreEqual(metadata, capture.Frames[0].Payload.ToArray());
    }

    [TestMethod]
    public void OpenSessionStream_EmittedFrame_CarriesStreamType()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.OpenSessionStream(streamType: 55u);

        Assert.AreEqual(55u, capture.Frames[0].StreamType);
    }

    [TestMethod]
    public void OpenSessionStream_EmittedFrame_HasNullRequestId()
    {
        // Session-scoped streams are not bound to any request.
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.OpenSessionStream();

        Assert.IsNull(capture.Frames[0].RequestId);
    }

    [TestMethod]
    public void OpenSessionStream_AppearsInSnapshot()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);

        var stream = session.Commands.OpenSessionStream();

        Assert.Contains(stream.StreamId, session.Diagnostics.GetSnapshot().OpenStreams);
    }

    [TestMethod]
    public void OpenSessionStream_ReturnedHandle_HasCorrectStreamType()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);

        var stream = session.Commands.OpenSessionStream(streamType: 42u);

        Assert.AreEqual(42u, stream.StreamType);
    }

    // ---------------------------------------------------------------
    // StreamData
    // ---------------------------------------------------------------

    [TestMethod]
    public void SendData_EmitsStreamDataFrame()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var stream = session.Commands.OpenSessionStream();
        using var capture = new OutboundFrameCapture(session);
        capture.Drain(); // discard StreamOpen

        stream.SendData(new byte[] { 0xDE, 0xAD });

        Assert.HasCount(1, capture.Frames);
        Assert.AreEqual(ProtocolFrameKind.StreamData, capture.Frames[0].Kind);
    }

    [TestMethod]
    public void SendData_EmittedFrame_HasCorrectStreamId()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var stream = session.Commands.OpenSessionStream();
        using var capture = new OutboundFrameCapture(session);
        capture.Drain();

        stream.SendData(new byte[] { 0x01 });

        Assert.AreEqual(stream.StreamId, capture.Frames[0].StreamId);
    }

    [TestMethod]
    public void SendData_EmittedFrame_CarriesCorrectPayload()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var stream = session.Commands.OpenSessionStream();
        using var capture = new OutboundFrameCapture(session);
        capture.Drain();
        var data = new byte[] { 0xDE, 0xAD };

        stream.SendData(data);

        CollectionAssert.AreEqual(data, capture.Frames[0].Payload.ToArray());
    }

    [TestMethod]
    public void SendData_MultipleFrames_EmittedInOrder()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var stream = session.Commands.OpenSessionStream();
        using var capture = new OutboundFrameCapture(session);
        capture.Drain();

        stream.SendData(new byte[] { 10 });
        stream.SendData(new byte[] { 20 });
        stream.SendData(new byte[] { 30 });

        var frames = capture.Frames;
        Assert.HasCount(3, frames);
        Assert.AreEqual((byte)10, frames[0].Payload.Span[0]);
        Assert.AreEqual((byte)20, frames[1].Payload.Span[0]);
        Assert.AreEqual((byte)30, frames[2].Payload.Span[0]);
    }

    // ---------------------------------------------------------------
    // StreamClose
    // ---------------------------------------------------------------

    [TestMethod]
    public void Close_EmitsStreamCloseFrame()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var stream = session.Commands.OpenSessionStream();
        using var capture = new OutboundFrameCapture(session);
        capture.Drain();

        stream.Close();

        Assert.HasCount(1, capture.Frames);
        Assert.AreEqual(ProtocolFrameKind.StreamClose, capture.Frames[0].Kind);
        Assert.AreEqual(stream.StreamId, capture.Frames[0].StreamId);
    }

    [TestMethod]
    public void Close_RemovesStreamFromSnapshot()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var stream = session.Commands.OpenSessionStream();

        stream.Close();

        Assert.DoesNotContain(stream.StreamId, session.Diagnostics.GetSnapshot().OpenStreams);
    }

    [TestMethod]
    public void Close_IsIdempotent_DoesNotEmitSecondFrame()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var stream = session.Commands.OpenSessionStream();

        stream.Close();

        using var capture = new OutboundFrameCapture(session);
        stream.Close(); // second call — must be silent

        Assert.IsEmpty(capture.Frames);
    }

    // ---------------------------------------------------------------
    // StreamAbort (outgoing)
    // ---------------------------------------------------------------

    [TestMethod]
    public void Abort_EmitsStreamAbortFrame()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var stream = session.Commands.OpenSessionStream();
        using var capture = new OutboundFrameCapture(session);
        capture.Drain();

        stream.Abort();

        Assert.HasCount(1, capture.Frames);
        Assert.AreEqual(ProtocolFrameKind.StreamAbort, capture.Frames[0].Kind);
        Assert.AreEqual(stream.StreamId, capture.Frames[0].StreamId);
    }

    [TestMethod]
    public void Abort_RemovesStreamFromSnapshot()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var stream = session.Commands.OpenSessionStream();

        stream.Abort();

        Assert.DoesNotContain(stream.StreamId, session.Diagnostics.GetSnapshot().OpenStreams);
    }

    [TestMethod]
    public void Abort_IsIdempotent_DoesNotEmitSecondFrame()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var stream = session.Commands.OpenSessionStream();

        stream.Abort();

        using var capture = new OutboundFrameCapture(session);
        stream.Abort();

        Assert.IsEmpty(capture.Frames);
    }

    // ---------------------------------------------------------------
    // OutgoingStream state guards
    // ---------------------------------------------------------------

    [TestMethod]
    public void SendData_AfterClose_ThrowsInvalidOperationException()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var stream = session.Commands.OpenSessionStream();
        stream.Close();

        Assert.Throws<InvalidOperationException>(() => stream.SendData(new byte[] { 0x01 }));
    }

    [TestMethod]
    public void SendData_AfterAbort_ThrowsInvalidOperationException()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var stream = session.Commands.OpenSessionStream();
        stream.Abort();

        Assert.Throws<InvalidOperationException>(() => stream.SendData(new byte[] { 0x01 }));
    }

    [TestMethod]
    public void MultipleDistinctStreams_HaveDistinctIds()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);

        var s1 = session.Commands.OpenSessionStream();
        var s2 = session.Commands.OpenSessionStream();
        var s3 = session.Commands.OpenSessionStream();

        Assert.AreNotEqual(s1.StreamId, s2.StreamId);
        Assert.AreNotEqual(s2.StreamId, s3.StreamId);
        Assert.AreNotEqual(s1.StreamId, s3.StreamId);
    }
}
