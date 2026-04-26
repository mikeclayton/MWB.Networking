using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

namespace _ProtocolSession;

/// <summary>
/// Tests for the Event frame kind:
/// fire-and-forget, terminal, no request/stream lifecycle.
/// </summary>
[TestClass]
public sealed partial class Events
{
    public TestContext TestContext
    {
        get;
        set;
    }

    [TestMethod]
    public void Event_DoesNotAffectSnapshot()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);

        var eventCount = 0;
        session.Observer.EventReceived += (_, _) => eventCount++;

        session.Runtime.ProcessFrame(ProtocolFrames.Event(1, new([0xFF])));

        var snap = session.Diagnostics.GetSnapshot();

        Assert.AreEqual(1, eventCount);
        Assert.IsEmpty(snap.OpenRequests);
        Assert.IsEmpty(snap.OpenStreams);
    }

    [TestMethod]
    public void Event_DoesNotProduceOutboundFrames()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var runtime = session.Runtime;

        session.Observer.EventReceived += (_, _) => { };

        runtime.ProcessFrame(ProtocolFrames.Event(1, new([0x2A])));

        Assert.IsEmpty(runtime.DrainOutboundFrames());
    }

    [TestMethod]
    public void Event_WithEmptyPayload_IsRaisedCorrectly()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var runtime = session.Runtime;

        var callCount = 0;
        var payloadLength = -1;

        session.Observer.EventReceived += (_, payload) =>
        {
            callCount++;
            payloadLength = payload.Length;
        };

        runtime.ProcessFrame(ProtocolFrames.Event(1));

        Assert.AreEqual(1, callCount);
        Assert.AreEqual(0, payloadLength);

        Assert.IsEmpty(runtime.DrainOutboundFrames());
    }

    [TestMethod]
    public void MultipleEvents_AreRaisedInOrder()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var runtime = session.Runtime;

        var received = new List<byte>();

        session.Observer.EventReceived += (_, payload) =>
        {
            received.Add(payload.Span[0]);
        };

        runtime.ProcessFrame(ProtocolFrames.Event(1, new byte[] { 1 }));
        runtime.ProcessFrame(ProtocolFrames.Event(1, new byte[] { 2 }));
        runtime.ProcessFrame(ProtocolFrames.Event(1, new byte[] { 3 }));

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, received);

        Assert.IsEmpty(runtime.DrainOutboundFrames());
    }

    [TestMethod]
    public void SingleEvent_IsRaisedToApplication_AndNotEmittedOutbound()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var runtime = session.Runtime;

        uint? receivedType = null;
        ReadOnlyMemory<byte> receivedPayload = default;
        var callCount = 0;

        session.Observer.EventReceived += (eventType, payload) =>
        {
            receivedType = eventType;
            receivedPayload = payload;
            callCount++;
        };

        var payload = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        runtime.ProcessFrame(ProtocolFrames.Event(1, payload));

        Assert.AreEqual(1, callCount);
        Assert.AreEqual(1u, receivedType);
        CollectionAssert.AreEqual(payload, receivedPayload.ToArray());

        Assert.IsEmpty(runtime.DrainOutboundFrames());
    }
}
