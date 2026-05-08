using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Session.Frames;
using MWB.Networking.Layer2_Protocol.Session.UnitTests.Helpers;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

namespace _ProtocolSession;

/// <summary>
/// Tests for the OutgoingStream frame-ordering contract:
/// StreamOpen must precede any StreamData, which must precede StreamClose/StreamAbort,
/// and all frames for a stream must carry the same StreamId.
/// </summary>
[TestClass]
public sealed partial class Streams_Lifecycle
{
    public TestContext TestContext { get; set; }

    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    [TestMethod]
    public void OpenSendClose_EmitsFramesInCorrectOrder()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        var stream = session.Commands.OpenSessionStream();
        stream.SendData(new byte[] { 10 });
        stream.SendData(new byte[] { 20 });
        stream.SendData(new byte[] { 30 });
        stream.Close();

        var frames = capture.Frames;
        Assert.HasCount(5, frames);
        Assert.AreEqual(ProtocolFrameKind.StreamOpen, frames[0].Kind);
        Assert.AreEqual(ProtocolFrameKind.StreamData, frames[1].Kind);
        Assert.AreEqual(ProtocolFrameKind.StreamData, frames[2].Kind);
        Assert.AreEqual(ProtocolFrameKind.StreamData, frames[3].Kind);
        Assert.AreEqual(ProtocolFrameKind.StreamClose, frames[4].Kind);
    }

    [TestMethod]
    public void OpenSendClose_AllFramesCarrySameStreamId()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        var stream = session.Commands.OpenSessionStream();
        stream.SendData(new byte[] { 1 });
        stream.Close();

        Assert.IsTrue(capture.Frames.All(f => f.StreamId == stream.StreamId),
            "Every frame in the stream lifecycle must carry the same StreamId.");
    }

    [TestMethod]
    public void OpenSendAbort_EmitsStreamAbortAsLastFrame()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        var stream = session.Commands.OpenSessionStream();
        stream.SendData(new byte[] { 0xFF });
        stream.Abort();

        var frames = capture.Frames;
        Assert.HasCount(3, frames);
        Assert.AreEqual(ProtocolFrameKind.StreamOpen, frames[0].Kind);
        Assert.AreEqual(ProtocolFrameKind.StreamData, frames[1].Kind);
        Assert.AreEqual(ProtocolFrameKind.StreamAbort, frames[2].Kind);
    }
}
