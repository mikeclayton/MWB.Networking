using MWB.Networking.Layer2_Protocol.Internal;

namespace MWB.Networking.Layer2_Protocol.UnitTests;

public partial class ProtocolSessionTests
{
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

        private static IProtocolSession CreateSession => new ProtocolSession();

        [TestMethod]
        public void SingleEvent_IsRaisedToApplication_AndNotEmittedOutbound()
        {
            var session = CreateSession;
            var runtime = (IProtocolSessionRuntime)session;

            uint? receivedType = null;
            ReadOnlyMemory<byte> receivedPayload = default;
            var callCount = 0;

            session.EventReceived += (eventType, payload) =>
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

        [TestMethod]
        public void MultipleEvents_AreRaisedInOrder()
        {
            var session = CreateSession;
            var runtime = (IProtocolSessionRuntime)session;

            var received = new List<byte>();

            session.EventReceived += (_, payload) =>
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
        public void Event_WithEmptyPayload_IsRaisedCorrectly()
        {
            var session = CreateSession;
            var runtime = (IProtocolSessionRuntime)session;

            var callCount = 0;
            var payloadLength = -1;

            session.EventReceived += (_, payload) =>
            {
                callCount++;
                payloadLength = payload.Length;
            };

            runtime.ProcessFrame(ProtocolFrames.Event(1, ReadOnlyMemory<byte>.Empty));

            Assert.AreEqual(1, callCount);
            Assert.AreEqual(0, payloadLength);

            Assert.IsEmpty(runtime.DrainOutboundFrames());
        }

        [TestMethod]
        public void Event_DoesNotAffectSnapshot()
        {
            var session = CreateSession;
            var runtime = (IProtocolSessionRuntime)session;

            session.EventReceived += (_, _) => { };

            runtime.ProcessFrame(ProtocolFrames.Event(1, new([0xFF])));

            var snap = session.Snapshot();

            Assert.IsEmpty(snap.OpenRequests);
            Assert.IsEmpty(snap.OpenStreams);
        }

        [TestMethod]
        public void Event_DoesNotProduceOutboundFrames()
        {
            var session = CreateSession;
            var runtime = (IProtocolSessionRuntime)session;

            session.EventReceived += (_, _) => { };

            runtime.ProcessFrame(ProtocolFrames.Event(1, new([0x2A])));

            Assert.IsEmpty(runtime.DrainOutboundFrames());
        }
    }
}
