using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Session.Frames;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

namespace _ProtocolSession;

/// <summary>
/// Tests for outbound Event sending via <see cref="Session.Api.IProtocolSessionCommands.SendEvent"/>.
///
/// Events are fire-and-forget frames: they carry an optional type discriminator and
/// an opaque payload, and have no associated request or stream lifecycle.
/// Sending an event must produce exactly one outbound frame and nothing else.
/// </summary>
[TestClass]
public sealed class Events_Outbound
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

    // ------------------------------------------------------------------
    // Frame structure
    // ------------------------------------------------------------------
    // Verify that the emitted frame has exactly the shape the protocol
    // specifies: kind=Event, the provided EventType and Payload, and no
    // request or stream correlation fields.

    [TestMethod]
    public void SendEvent_EmitsSingleFrame()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.SendEvent(1u, new byte[] { 0x01 });

        Assert.HasCount(1, capture.Frames);
    }

    [TestMethod]
    public void SendEvent_EmittedFrame_HasKindEvent()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.SendEvent(1u, new byte[] { 0x01 });

        Assert.AreEqual(ProtocolFrameKind.Event, capture.Frames[0].Kind);
    }

    [TestMethod]
    public void SendEvent_EmittedFrame_CarriesCorrectEventType()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.SendEvent(42u, new byte[] { 0x01 });

        Assert.AreEqual(42u, capture.Frames[0].EventType);
    }

    [TestMethod]
    public void SendEvent_EmittedFrame_CarriesCorrectPayload()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);
        var payload = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        session.Commands.SendEvent(1u, payload);

        CollectionAssert.AreEqual(payload, capture.Frames[0].Payload.ToArray());
    }

    [TestMethod]
    public void SendEvent_EmittedFrame_HasNullRequestId()
    {
        // Events are not correlated with any request lifecycle.
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.SendEvent(1u, new byte[] { 0x01 });

        Assert.IsNull(capture.Frames[0].RequestId);
    }

    [TestMethod]
    public void SendEvent_EmittedFrame_HasNullStreamId()
    {
        // Events are not associated with any stream.
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.SendEvent(1u, new byte[] { 0x01 });

        Assert.IsNull(capture.Frames[0].StreamId);
    }

    // ------------------------------------------------------------------
    // Nullable / default variants
    // ------------------------------------------------------------------
    // EventType is uint? throughout the stack. Null is a valid value by
    // design — ProtocolFrames.Event(uint? eventType = null) explicitly
    // accepts it. Sending with null EventType must succeed and produce
    // a frame whose EventType is null.

    [TestMethod]
    public void SendEvent_WithNullEventType_IsPermitted()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.SendEvent(null, new byte[] { 0x01 });

        Assert.HasCount(1, capture.Frames);
        Assert.AreEqual(ProtocolFrameKind.Event, capture.Frames[0].Kind);
        Assert.IsNull(capture.Frames[0].EventType);
    }

    [TestMethod]
    public void SendEvent_WithEmptyPayload_EmitsFrameWithEmptyPayload()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.SendEvent(1u, ReadOnlyMemory<byte>.Empty);

        Assert.HasCount(1, capture.Frames);
        Assert.AreEqual(0, capture.Frames[0].Payload.Length);
    }

    [TestMethod]
    public void SendEvent_WithDefaultPayload_EmitsFrameWithEmptyPayload()
    {
        // Calling the overload that omits payload should produce an empty payload.
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.SendEvent(1u);

        Assert.HasCount(1, capture.Frames);
        Assert.AreEqual(0, capture.Frames[0].Payload.Length);
    }

    [TestMethod]
    public void SendEvent_TypelessOverload_EmitsNullEventType()
    {
        // SendEvent(payload) is a convenience overload that passes null as the EventType.
        // The emitted frame should therefore carry a null EventType.
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.SendEvent(new byte[] { 0xAB });

        Assert.HasCount(1, capture.Frames);
        Assert.IsNull(capture.Frames[0].EventType);
        CollectionAssert.AreEqual(new byte[] { 0xAB }, capture.Frames[0].Payload.ToArray());
    }

    // ------------------------------------------------------------------
    // Multiple sends
    // ------------------------------------------------------------------

    [TestMethod]
    public void SendEvent_MultipleCalls_AllFramesEmittedInOrder()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.SendEvent(1u, new byte[] { 0xAA });
        session.Commands.SendEvent(2u, new byte[] { 0xBB });
        session.Commands.SendEvent(3u, new byte[] { 0xCC });

        var frames = capture.Frames;

        Assert.HasCount(3, frames);
        Assert.AreEqual(1u, frames[0].EventType);
        Assert.AreEqual(2u, frames[1].EventType);
        Assert.AreEqual(3u, frames[2].EventType);
    }

    [TestMethod]
    public void SendEvent_MultipleCalls_AllFramesHaveKindEvent()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.SendEvent(1u, new byte[] { 0xAA });
        session.Commands.SendEvent(2u, new byte[] { 0xBB });
        session.Commands.SendEvent(3u, new byte[] { 0xCC });

        Assert.IsTrue(capture.Frames.All(f => f.Kind == ProtocolFrameKind.Event),
            "Every emitted frame must have Kind == Event");
    }

    [TestMethod]
    public void SendEvent_MultipleCalls_EachFrameCarriesDistinctPayload()
    {
        // Payloads must not bleed into each other — each frame carries its own bytes.
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        using var capture = new OutboundFrameCapture(session);

        session.Commands.SendEvent(1u, new byte[] { 0x11 });
        session.Commands.SendEvent(2u, new byte[] { 0x22 });
        session.Commands.SendEvent(3u, new byte[] { 0x33 });

        var frames = capture.Frames;

        CollectionAssert.AreEqual(new byte[] { 0x11 }, frames[0].Payload.ToArray());
        CollectionAssert.AreEqual(new byte[] { 0x22 }, frames[1].Payload.ToArray());
        CollectionAssert.AreEqual(new byte[] { 0x33 }, frames[2].Payload.ToArray());
    }

    // ------------------------------------------------------------------
    // Side-effect isolation
    // ------------------------------------------------------------------
    // Sending an event must have no observable side effects beyond the
    // emitted outbound frame: it must not trigger the inbound handler on
    // the same session, and it must not alter request or stream state.

    [TestMethod]
    public void SendEvent_DoesNotFireInboundEventReceivedHandler()
    {
        // Outbound events must not loop back and invoke EventReceived on the
        // same session. The handler is for frames arriving from the peer only.
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);

        var callCount = 0;
        session.Observer.EventReceived += (_, _) => callCount++;

        session.Commands.SendEvent(1u, new byte[] { 0x01 });

        Assert.AreEqual(0, callCount);
    }

    [TestMethod]
    public void SendEvent_DoesNotCreateOpenRequests()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);

        session.Commands.SendEvent(1u, new byte[] { 0x01 });

        Assert.IsEmpty(session.Diagnostics.GetSnapshot().OpenRequests);
    }

    [TestMethod]
    public void SendEvent_DoesNotCreateOpenStreams()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);

        session.Commands.SendEvent(1u, new byte[] { 0x01 });

        Assert.IsEmpty(session.Diagnostics.GetSnapshot().OpenStreams);
    }

    [TestMethod]
    public void SendEvent_WhileRequestIsOpen_DoesNotAffectRequest()
    {
        // Sending an event while a request is open must leave the request
        // lifecycle completely untouched.
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));
        session.Commands.SendEvent(99u, new byte[] { 0xFF });

        var snap = session.Diagnostics.GetSnapshot();
        Assert.Contains(1u, snap.OpenRequests);
    }

    [TestMethod]
    public void SendEvent_WhileStreamIsOpen_DoesNotAffectStream()
    {
        // Sending an event while a stream is open must leave the stream
        // lifecycle completely untouched.
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(10));
        session.Commands.SendEvent(99u, new byte[] { 0xFF });

        var snap = session.Diagnostics.GetSnapshot();
        Assert.Contains(10u, snap.OpenStreams);
    }
}
