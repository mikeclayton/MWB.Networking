using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Session.Frames;
using MWB.Networking.Layer2_Protocol.Session.UnitTests.Helpers;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

namespace _ProtocolSession;

/// <summary>
/// Tests for <see cref="Session.Api.IProtocolSessionProcessor.ProcessFrame"/> routing:
/// null frames, unknown frame kinds, and correct dispatch to the right sub-system.
/// </summary>
[TestClass]
public sealed class Session_FrameRouting
{
    public TestContext TestContext { get; set; }

    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    // ---------------------------------------------------------------
    // Null guard
    // ---------------------------------------------------------------

    [TestMethod]
    public void ProcessFrame_NullFrame_ThrowsArgumentNullException()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        Assert.Throws<ArgumentNullException>(() => processor.ProcessFrame(null!));
    }

    // ---------------------------------------------------------------
    // Unknown frame kind
    // ---------------------------------------------------------------

    [TestMethod]
    public void ProcessFrame_UnknownFrameKind_ThrowsProtocolException()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var frame = ProtocolFrameGenerator.CreateInvalidProtocolFrame((ProtocolFrameKind)0xFF);

        Assert.Throws<ProtocolException>(() => processor.ProcessFrame(frame));
    }

    [TestMethod]
    public void ProcessFrame_UnknownFrameKind_ErrorKind_IsUnknownFrameKind()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var frame = ProtocolFrameGenerator.CreateInvalidProtocolFrame((ProtocolFrameKind)0xFF);

        var ex = Assert.Throws<ProtocolException>(() => processor.ProcessFrame(frame));

        Assert.AreEqual(ProtocolErrorKind.UnknownFrameKind, ex.ErrorKind);
    }

    // ---------------------------------------------------------------
    // Frame routing — correct sub-system receives each kind
    // ---------------------------------------------------------------

    [TestMethod]
    public void ProcessFrame_EventKind_RoutesToEventObserver()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var raised = false;
        session.Observer.EventReceived += (_, _) => raised = true;

        processor.ProcessFrame(ProtocolFrames.Event(1u));

        Assert.IsTrue(raised);
    }

    [TestMethod]
    public void ProcessFrame_RequestKind_RoutesToRequestObserver()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var raised = false;
        session.Observer.RequestReceived += (_, _) => raised = true;

        processor.ProcessFrame(ProtocolFrames.Request(1));

        Assert.IsTrue(raised);
    }

    [TestMethod]
    public void ProcessFrame_StreamOpenKind_RoutesToStreamObserver()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var raised = false;
        session.Observer.StreamOpened += (_, _) => raised = true;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));

        Assert.IsTrue(raised);
    }

    [TestMethod]
    public void ProcessFrame_StreamDataKind_RoutesToStreamDataObserver()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var raised = false;
        session.Observer.StreamDataReceived += (_, _) => raised = true;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.StreamData(2, new byte[] { 0x01 }));

        Assert.IsTrue(raised);
    }

    [TestMethod]
    public void ProcessFrame_StreamCloseKind_RoutesToStreamClosedObserver()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var raised = false;
        session.Observer.StreamClosed += (_, _) => raised = true;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.StreamClose(2));

        Assert.IsTrue(raised);
    }

    [TestMethod]
    public void ProcessFrame_StreamAbortKind_RoutesToStreamAbortedObserver()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var raised = false;
        session.Observer.StreamAborted += (_, _) => raised = true;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.StreamAbort(2));

        Assert.IsTrue(raised);
    }

    // ---------------------------------------------------------------
    // Frame routing does not cross-contaminate sub-systems
    // ---------------------------------------------------------------

    [TestMethod]
    public void ProcessFrame_EventKind_DoesNotRaiseRequestOrStreamObservers()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var requestRaised = false;
        var streamRaised = false;
        session.Observer.RequestReceived += (_, _) => requestRaised = true;
        session.Observer.StreamOpened += (_, _) => streamRaised = true;

        processor.ProcessFrame(ProtocolFrames.Event(1u));

        Assert.IsFalse(requestRaised);
        Assert.IsFalse(streamRaised);
    }

    [TestMethod]
    public void ProcessFrame_RequestKind_DoesNotRaiseEventOrStreamObservers()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var eventRaised = false;
        var streamRaised = false;
        session.Observer.EventReceived += (_, _) => eventRaised = true;
        session.Observer.StreamOpened += (_, _) => streamRaised = true;

        processor.ProcessFrame(ProtocolFrames.Request(1));

        Assert.IsFalse(eventRaised);
        Assert.IsFalse(streamRaised);
    }
}
