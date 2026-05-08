using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Session.Frames;
using MWB.Networking.Layer2_Protocol.Session.UnitTests.Helpers;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

namespace _ProtocolSession;

[TestClass]
public sealed partial class Streams_Interleaving
{
    public TestContext TestContext { get; set; }

    [TestCleanup]
    public void Cleanup() { GC.Collect(); GC.WaitForPendingFinalizers(); }

    [TestMethod]
    public void InterleavedSessionStreams_FramesEmittedInOrder()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);
        var s1 = session.Commands.OpenSessionStream();
        var s2 = session.Commands.OpenSessionStream();
        s1.SendData(new byte[] { 0x01 });
        s2.SendData(new byte[] { 0x02 });
        s1.Close();
        s2.Close();
        var frames = capture.Frames;
        Assert.HasCount(6, frames);
        Assert.AreEqual(ProtocolFrameKind.StreamOpen, frames[0].Kind);
        Assert.AreEqual(ProtocolFrameKind.StreamOpen, frames[1].Kind);
        Assert.AreEqual(ProtocolFrameKind.StreamData, frames[2].Kind);
        Assert.AreEqual(ProtocolFrameKind.StreamData, frames[3].Kind);
        Assert.AreEqual(ProtocolFrameKind.StreamClose, frames[4].Kind);
        Assert.AreEqual(ProtocolFrameKind.StreamClose, frames[5].Kind);
    }

    [TestMethod]
    public void InterleavedSessionStreams_EachFrameCarriesCorrectStreamId()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);
        var s1 = session.Commands.OpenSessionStream();
        var s2 = session.Commands.OpenSessionStream();
        s1.SendData(new byte[] { 0x01 });
        s2.SendData(new byte[] { 0x02 });
        s1.SendData(new byte[] { 0x03 });
        s2.SendData(new byte[] { 0x04 });
        s1.Close();
        s2.Close();
        var frames = capture.Frames;
        Assert.HasCount(8, frames);
        Assert.AreEqual(s1.StreamId, frames[0].StreamId);
        Assert.AreEqual(s2.StreamId, frames[1].StreamId);
        Assert.AreEqual(s1.StreamId, frames[2].StreamId);
        Assert.AreEqual(s2.StreamId, frames[3].StreamId);
        Assert.AreEqual(s1.StreamId, frames[4].StreamId);
        Assert.AreEqual(s2.StreamId, frames[5].StreamId);
        Assert.AreEqual(s1.StreamId, frames[6].StreamId);
        Assert.AreEqual(s2.StreamId, frames[7].StreamId);
    }

    [TestMethod]
    public void SessionStream_OpenedIndependentlyOfRequest_DoesNotAffectRequest()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;
        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        var snap = session.Diagnostics.GetSnapshot();
        Assert.Contains(1u, snap.OpenRequests);
        Assert.Contains(2u, snap.OpenStreams);
    }

    [TestMethod]
    public void ClosingRequest_DoesNotAffectSessionScopedStream()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;
        session.Observer.RequestReceived += (req, _) => req.Respond();
        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.Request(1));
        var snap = session.Diagnostics.GetSnapshot();
        Assert.IsEmpty(snap.OpenRequests);
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
    public void InboundEvents_DoNotAffectOpenStream()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;
        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.Event(99u));
        processor.ProcessFrame(ProtocolFrames.Event(100u));
        Assert.Contains(2u, session.Diagnostics.GetSnapshot().OpenStreams);
    }
}