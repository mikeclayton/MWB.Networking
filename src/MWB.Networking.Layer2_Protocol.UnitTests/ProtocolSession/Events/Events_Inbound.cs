using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Events.Api;
using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

namespace _ProtocolSession;

/// <summary>
/// Tests for inbound Event processing: frames arriving from the peer via
/// <see cref="Session.Api.IProtocolSessionProcessor.ProcessFrame"/> and surfaced
/// through <see cref="Session.Api.IProtocolSessionObserver.EventReceived"/>.
///
/// Events are fire-and-forget. Receiving one must raise the observer exactly
/// once per frame, with the correct EventType and Payload, and produce no
/// outbound frames and no change to request or stream state.
/// </summary>
[TestClass]
public sealed class Events_Inbound
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
    // Handler invocation — basics
    // ------------------------------------------------------------------

    [TestMethod]
    public void InboundEvent_EventReceivedHandler_IsCalledExactlyOnce()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var callCount = 0;
        session.Observer.EventReceived += (_, _) => callCount++;

        processor.ProcessFrame(ProtocolFrames.Event(1u, new byte[] { 0x01 }));

        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public void InboundEvent_IncomingEvent_IsNotNull()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        IncomingEvent? received = null;
        session.Observer.EventReceived += (@event, _) => received = @event;

        processor.ProcessFrame(ProtocolFrames.Event(1u, new byte[] { 0x01 }));

        Assert.IsNotNull(received);
    }

    [TestMethod]
    public void InboundEvent_IncomingEvent_EventType_MatchesFrame()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        uint? receivedType = null;
        session.Observer.EventReceived += (@event, _) => receivedType = @event.EventType;

        processor.ProcessFrame(ProtocolFrames.Event(77u));

        Assert.AreEqual(77u, receivedType);
    }

    [TestMethod]
    public void InboundEvent_Payload_MatchesFrame()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        ReadOnlyMemory<byte> receivedPayload = default;
        session.Observer.EventReceived += (_, payload) => receivedPayload = payload;

        var sent = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        processor.ProcessFrame(ProtocolFrames.Event(1u, sent));

        CollectionAssert.AreEqual(sent, receivedPayload.ToArray());
    }

    [TestMethod]
    public void InboundEvent_WithEmptyPayload_HandlerReceivesEmptyPayload()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var receivedLength = -1;
        session.Observer.EventReceived += (_, payload) => receivedLength = payload.Length;

        processor.ProcessFrame(ProtocolFrames.Event(1u));

        Assert.AreEqual(0, receivedLength);
    }

    // ------------------------------------------------------------------
    // Null EventType — design intent
    // ------------------------------------------------------------------
    // EventType is uint? throughout the entire protocol stack.
    // ProtocolFrames.Event(uint? eventType = null) explicitly permits null,
    // and IncomingEvent.EventType is also uint?, so null is a fully valid
    // received value. The EventReceived handler must be called, and
    // IncomingEvent.EventType must be null.

    [TestMethod]
    public void InboundEvent_WithNullEventType_DoesNotThrow()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        session.Observer.EventReceived += (_, _) => { };

        // Should complete without throwing — null EventType is valid by design.
        processor.ProcessFrame(ProtocolFrames.Event(null));
    }

    [TestMethod]
    public void InboundEvent_WithNullEventType_HandlerIsCalledWithNullEventType()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var called = false;
        // Use a sentinel so we can distinguish "handler not called" from "handler
        // called with null". The initial value 0xDEAD is never a valid result.
        uint? receivedType = 0xDEAD;

        session.Observer.EventReceived += (@event, _) =>
        {
            called = true;
            receivedType = @event.EventType;
        };

        processor.ProcessFrame(ProtocolFrames.Event(null));

        Assert.IsTrue(called, "EventReceived should be raised for a null-EventType event");
        Assert.IsNull(receivedType, "IncomingEvent.EventType should be null when the frame carries no EventType");
    }

    // ------------------------------------------------------------------
    // Multiple events / multiple handlers
    // ------------------------------------------------------------------

    [TestMethod]
    public void InboundEvent_MultipleHandlers_AllReceiveTheSameEvent()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var countA = 0;
        var countB = 0;
        var countC = 0;

        session.Observer.EventReceived += (_, _) => countA++;
        session.Observer.EventReceived += (_, _) => countB++;
        session.Observer.EventReceived += (_, _) => countC++;

        processor.ProcessFrame(ProtocolFrames.Event(1u, new byte[] { 0x42 }));

        Assert.AreEqual(1, countA);
        Assert.AreEqual(1, countB);
        Assert.AreEqual(1, countC);
    }

    [TestMethod]
    public void InboundEvent_MultipleEvents_HandlerCalledOncePerEvent()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var callCount = 0;
        session.Observer.EventReceived += (_, _) => callCount++;

        processor.ProcessFrame(ProtocolFrames.Event(1u, new byte[] { 0x01 }));
        processor.ProcessFrame(ProtocolFrames.Event(1u, new byte[] { 0x02 }));
        processor.ProcessFrame(ProtocolFrames.Event(1u, new byte[] { 0x03 }));

        Assert.AreEqual(3, callCount);
    }

    [TestMethod]
    public void InboundEvent_MultipleEvents_DeliveredInOrder()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var receivedPayloads = new List<byte>();
        session.Observer.EventReceived += (_, payload) => receivedPayloads.Add(payload.Span[0]);

        processor.ProcessFrame(ProtocolFrames.Event(1u, new byte[] { 0x0A }));
        processor.ProcessFrame(ProtocolFrames.Event(1u, new byte[] { 0x0B }));
        processor.ProcessFrame(ProtocolFrames.Event(1u, new byte[] { 0x0C }));

        CollectionAssert.AreEqual(new byte[] { 0x0A, 0x0B, 0x0C }, receivedPayloads);
    }

    [TestMethod]
    public void InboundEvent_DifferentEventTypes_EachDeliveredWithCorrectType()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var receivedTypes = new List<uint?>();
        session.Observer.EventReceived += (@event, _) => receivedTypes.Add(@event.EventType);

        processor.ProcessFrame(ProtocolFrames.Event(10u));
        processor.ProcessFrame(ProtocolFrames.Event(20u));
        processor.ProcessFrame(ProtocolFrames.Event(30u));

        CollectionAssert.AreEqual(new uint?[] { 10u, 20u, 30u }, receivedTypes);
    }

    // ------------------------------------------------------------------
    // No handler
    // ------------------------------------------------------------------

    [TestMethod]
    public void InboundEvent_WithNoHandlers_DoesNotThrow()
    {
        // Events are fire-and-forget. There is no requirement for a handler
        // to be registered — receiving an event with no subscribers must
        // complete silently.
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        // No handler registered — should not throw.
        processor.ProcessFrame(ProtocolFrames.Event(1u, new byte[] { 0x01 }));
    }

    // ------------------------------------------------------------------
    // No outbound side effects
    // ------------------------------------------------------------------
    // Events are one-way. The receiver must not emit any acknowledgment,
    // error, or any other outbound frame in response.

    [TestMethod]
    public void InboundEvent_DoesNotProduceOutboundFrames()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        session.Observer.EventReceived += (_, _) => { };

        processor.ProcessFrame(ProtocolFrames.Event(1u, new byte[] { 0x01 }));

        Assert.IsEmpty(processor.DrainOutboundFrames());
    }

    [TestMethod]
    public void InboundEvent_MultipleEvents_ProduceNoOutboundFrames()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Event(1u, new byte[] { 0x01 }));
        processor.ProcessFrame(ProtocolFrames.Event(2u, new byte[] { 0x02 }));
        processor.ProcessFrame(ProtocolFrames.Event(3u, new byte[] { 0x03 }));

        Assert.IsEmpty(processor.DrainOutboundFrames());
    }

    // ------------------------------------------------------------------
    // No snapshot side effects
    // ------------------------------------------------------------------
    // Receiving events must not alter request or stream state.

    [TestMethod]
    public void InboundEvent_DoesNotCreateOpenRequests()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Event(1u, new byte[] { 0x01 }));

        Assert.IsEmpty(session.Diagnostics.GetSnapshot().OpenRequests);
    }

    [TestMethod]
    public void InboundEvent_DoesNotCreateOpenStreams()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Event(1u, new byte[] { 0x01 }));

        Assert.IsEmpty(session.Diagnostics.GetSnapshot().OpenStreams);
    }

    // ------------------------------------------------------------------
    // Independence from other protocol activity
    // ------------------------------------------------------------------
    // Events share the same protocol channel as requests and streams.
    // Receiving events must not perturb any concurrent request or stream
    // lifecycle, and concurrent request/stream activity must not suppress
    // event delivery.

    [TestMethod]
    public void InboundEvent_WhileRequestIsOpen_RequestIsUnaffected()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));

        processor.ProcessFrame(ProtocolFrames.Event(99u, new byte[] { 0xFF }));

        Assert.Contains(1u, session.Diagnostics.GetSnapshot().OpenRequests);
    }

    [TestMethod]
    public void InboundEvent_WhileStreamIsOpen_StreamIsUnaffected()
    {
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(10));

        processor.ProcessFrame(ProtocolFrames.Event(99u, new byte[] { 0xFF }));

        Assert.Contains(10u, session.Diagnostics.GetSnapshot().OpenStreams);
    }

    [TestMethod]
    public void InboundEvent_InterleavedWithRequests_AllEventsDelivered()
    {
        // Events arriving between request frames must all be delivered,
        // with no event dropped or swallowed by request processing.
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var receivedTypes = new List<uint?>();
        session.Observer.EventReceived += (@event, _) => receivedTypes.Add(@event.EventType);

        processor.ProcessFrame(ProtocolFrames.Event(1u));
        processor.ProcessFrame(ProtocolFrames.Request(10));
        processor.ProcessFrame(ProtocolFrames.Event(2u));
        processor.ProcessFrame(ProtocolFrames.Request(20));
        processor.ProcessFrame(ProtocolFrames.Event(3u));
        processor.ProcessFrame(ProtocolFrames.Response(10));
        processor.ProcessFrame(ProtocolFrames.Event(4u));

        CollectionAssert.AreEqual(new uint?[] { 1u, 2u, 3u, 4u }, receivedTypes);
    }

    [TestMethod]
    public void InboundEvent_InterleavedWithRequests_RequestsUnaffected()
    {
        // Request state must be unaffected by interleaved events.
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(10));
        processor.ProcessFrame(ProtocolFrames.Event(99u));
        processor.ProcessFrame(ProtocolFrames.Request(20));
        processor.ProcessFrame(ProtocolFrames.Event(99u));

        var snap = session.Diagnostics.GetSnapshot();

        Assert.HasCount(2, snap.OpenRequests);
        Assert.Contains(10u, snap.OpenRequests);
        Assert.Contains(20u, snap.OpenRequests);
    }

    [TestMethod]
    public void InboundEvent_InterleavedWithStreams_AllEventsDelivered()
    {
        // Events arriving around stream open/data/close frames must all be
        // delivered in order.
        var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
        var processor = session.Processor;

        var receivedTypes = new List<uint?>();
        session.Observer.EventReceived += (@event, _) => receivedTypes.Add(@event.EventType);

        processor.ProcessFrame(ProtocolFrames.Event(1u));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(10));
        processor.ProcessFrame(ProtocolFrames.Event(2u));
        processor.ProcessFrame(ProtocolFrames.StreamData(10, new byte[] { 0xAB }));
        processor.ProcessFrame(ProtocolFrames.Event(3u));
        processor.ProcessFrame(ProtocolFrames.StreamClose(10));
        processor.ProcessFrame(ProtocolFrames.Event(4u));

        CollectionAssert.AreEqual(new uint?[] { 1u, 2u, 3u, 4u }, receivedTypes);
    }
}
