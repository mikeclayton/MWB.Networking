using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Session.Frames;
using MWB.Networking.Layer2_Protocol.Session.Streams.Api;
using MWB.Networking.Layer2_Protocol.Session.UnitTests.Helpers;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

namespace _ProtocolSession;

/// <summary>
/// Tests for inbound stream processing: StreamOpen / StreamData / StreamClose / StreamAbort
/// frames arriving from the peer, and the local <see cref="IncomingStream.Abort"/> operation
/// that sends a StreamAbort outbound.
/// </summary>
[TestClass]
public sealed partial class Streams_Inbound
{
    public TestContext TestContext { get; set; }

    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    // ---------------------------------------------------------------
    // Snapshot tracking
    // ---------------------------------------------------------------

    [TestMethod]
    public void InboundStreamOpen_AppearsInSnapshot()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));

        Assert.Contains(2u, session.Diagnostics.GetSnapshot().OpenStreams);
    }

    [TestMethod]
    public void InboundStreamData_DoesNotCloseStream()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.StreamData(2, new byte[] { 0xAB }));

        Assert.Contains(2u, session.Diagnostics.GetSnapshot().OpenStreams);
    }

    [TestMethod]
    public void InboundStreamData_MultipleFrames_StreamRemainsOpen()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.StreamData(2, new byte[] { 1 }));
        processor.ProcessFrame(ProtocolFrames.StreamData(2, new byte[] { 2 }));
        processor.ProcessFrame(ProtocolFrames.StreamData(2, new byte[] { 3 }));

        Assert.Contains(2u, session.Diagnostics.GetSnapshot().OpenStreams);
    }

    [TestMethod]
    public void InboundStreamClose_RemovesStreamFromSnapshot()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.StreamClose(2));

        Assert.IsEmpty(session.Diagnostics.GetSnapshot().OpenStreams);
    }

    [TestMethod]
    public void MultipleConcurrentStreams_AllTrackedInSnapshot()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(4));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(6));

        var snap = session.Diagnostics.GetSnapshot();

        Assert.HasCount(3, snap.OpenStreams);
        Assert.Contains(2u, snap.OpenStreams);
        Assert.Contains(4u, snap.OpenStreams);
        Assert.Contains(6u, snap.OpenStreams);
    }

    [TestMethod]
    public void MultipleStreams_CloseIndependently()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(4));
        processor.ProcessFrame(ProtocolFrames.StreamClose(2));

        var snap = session.Diagnostics.GetSnapshot();

        Assert.HasCount(1, snap.OpenStreams);
        Assert.DoesNotContain(2u, snap.OpenStreams);
        Assert.Contains(4u, snap.OpenStreams);
    }

    [TestMethod]
    public void StreamId_ReusableAfterClose()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.StreamClose(2));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));

        Assert.Contains(2u, session.Diagnostics.GetSnapshot().OpenStreams);
    }

    // ---------------------------------------------------------------
    // No outbound side-effects
    // ---------------------------------------------------------------

    [TestMethod]
    public void InboundStreamData_DoesNotEmitOutboundFrames()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;
        using var capture = new OutboundFrameCapture(session);

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.StreamData(2, new byte[] { 10 }));
        processor.ProcessFrame(ProtocolFrames.StreamData(2, new byte[] { 20 }));

        Assert.IsEmpty(capture.Frames);
    }

    [TestMethod]
    public void InboundStreamOpenDataClose_DoNotEmitOutboundFrames()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;
        using var capture = new OutboundFrameCapture(session);

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.StreamData(2, new byte[] { 0x01 }));
        processor.ProcessFrame(ProtocolFrames.StreamClose(2));

        Assert.IsEmpty(capture.Frames);
    }

    // ---------------------------------------------------------------
    // Observer events — StreamOpened
    // ---------------------------------------------------------------

    [TestMethod]
    public void InboundStreamOpen_RaisesStreamOpenedEvent()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var callCount = 0;
        session.Observer.StreamOpened += (_, _) => callCount++;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));

        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public void InboundStreamOpen_StreamOpenedEvent_ReceivesCorrectStream()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingStream? received = null;
        session.Observer.StreamOpened += (s, _) => received = s;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));

        Assert.IsNotNull(received);
        Assert.AreEqual(2u, received.StreamId);
    }

    [TestMethod]
    public void InboundStreamOpen_StreamOpenedEvent_ReceivesMetadata()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        StreamMetadata? receivedMetadata = null;
        session.Observer.StreamOpened += (_, m) => receivedMetadata = m;

        var metadata = new byte[] { 0xAA, 0xBB };
        processor.ProcessFrame(ProtocolFrames.StreamOpen(2, metadata: metadata));

        Assert.IsNotNull(receivedMetadata);
        CollectionAssert.AreEqual(metadata, receivedMetadata.Payload.ToArray());
    }

    [TestMethod]
    public void InboundStreamOpen_WithStreamType_StreamOpenedEvent_ReceivesCorrectType()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingStream? received = null;
        session.Observer.StreamOpened += (s, _) => received = s;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2, streamType: 77u));

        Assert.AreEqual(77u, received!.StreamType);
    }

    // ---------------------------------------------------------------
    // Observer events — StreamDataReceived
    // ---------------------------------------------------------------

    [TestMethod]
    public void InboundStreamData_RaisesStreamDataReceivedEvent()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var callCount = 0;
        session.Observer.StreamDataReceived += (_, _) => callCount++;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.StreamData(2, new byte[] { 0x01 }));

        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public void InboundStreamData_StreamDataReceivedEvent_CarriesCorrectPayload()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        ReadOnlyMemory<byte> receivedPayload = default;
        session.Observer.StreamDataReceived += (_, payload) => receivedPayload = payload;

        var sent = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.StreamData(2, sent));

        CollectionAssert.AreEqual(sent, receivedPayload.ToArray());
    }

    [TestMethod]
    public void InboundStreamData_MultipleFrames_EventRaisedOncePerFrame()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var payloads = new List<byte>();
        session.Observer.StreamDataReceived += (_, payload) => payloads.Add(payload.Span[0]);

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.StreamData(2, new byte[] { 0x0A }));
        processor.ProcessFrame(ProtocolFrames.StreamData(2, new byte[] { 0x0B }));
        processor.ProcessFrame(ProtocolFrames.StreamData(2, new byte[] { 0x0C }));

        CollectionAssert.AreEqual(new byte[] { 0x0A, 0x0B, 0x0C }, payloads);
    }

    // ---------------------------------------------------------------
    // Observer events — StreamClosed
    // ---------------------------------------------------------------

    [TestMethod]
    public void InboundStreamClose_RaisesStreamClosedEvent()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var callCount = 0;
        session.Observer.StreamClosed += (_, _) => callCount++;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.StreamClose(2));

        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public void InboundStreamClose_StreamClosedEvent_ReceivesCorrectStream()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingStream? closedStream = null;
        session.Observer.StreamClosed += (s, _) => closedStream = s;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.StreamClose(2));

        Assert.IsNotNull(closedStream);
        Assert.AreEqual(2u, closedStream.StreamId);
    }

    // ---------------------------------------------------------------
    // Local abort of a peer's stream — IncomingStream.Abort()
    // ---------------------------------------------------------------

    [TestMethod]
    public void IncomingStream_Abort_EmitsStreamAbortFrame()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingStream? stream = null;
        session.Observer.StreamOpened += (s, _) => stream = s;
        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));

        using var capture = new OutboundFrameCapture(session);
        stream!.Abort();

        Assert.HasCount(1, capture.Frames);
        Assert.AreEqual(ProtocolFrameKind.StreamAbort, capture.Frames[0].Kind);
        Assert.AreEqual(2u, capture.Frames[0].StreamId);
    }

    [TestMethod]
    public void IncomingStream_Abort_RemovesStreamFromSnapshot()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingStream? stream = null;
        session.Observer.StreamOpened += (s, _) => stream = s;
        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));

        stream!.Abort();

        Assert.DoesNotContain(2u, session.Diagnostics.GetSnapshot().OpenStreams);
    }

    [TestMethod]
    public void IncomingStream_Abort_IsIdempotent()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingStream? stream = null;
        session.Observer.StreamOpened += (s, _) => stream = s;
        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));

        stream!.Abort();

        // Second Abort must not throw and must not emit a second frame.
        using var capture = new OutboundFrameCapture(session);
        stream.Abort();

        Assert.IsEmpty(capture.Frames);
    }

    // ---------------------------------------------------------------
    // Peer-initiated StreamAbort (Bug: NotImplementedException)
    // ---------------------------------------------------------------

    [TestMethod]
    public void InboundStreamAbort_RemovesStreamFromSnapshot()
    {
        // BUG: ProcessIncomingStreamAbortFrame currently throws NotImplementedException.
        // This test exposes the bug; it will pass once the implementation is complete.
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        Assert.Contains(2u, session.Diagnostics.GetSnapshot().OpenStreams);

        processor.ProcessFrame(ProtocolFrames.StreamAbort(2));

        Assert.DoesNotContain(2u, session.Diagnostics.GetSnapshot().OpenStreams);
    }

    [TestMethod]
    public void InboundStreamAbort_RaisesStreamAbortedEvent()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var callCount = 0;
        IncomingStream? abortedStream = null;
        session.Observer.StreamAborted += (s, _) =>
        {
            callCount++;
            abortedStream = s;
        };

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.StreamAbort(2));

        Assert.AreEqual(1, callCount);
        Assert.IsNotNull(abortedStream);
        Assert.AreEqual(2u, abortedStream.StreamId);
    }

    [TestMethod]
    public void InboundStreamAbort_DoesNotEmitOutboundFrames()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));

        using var capture = new OutboundFrameCapture(session);
        processor.ProcessFrame(ProtocolFrames.StreamAbort(2));

        Assert.IsEmpty(capture.Frames);
    }
}
