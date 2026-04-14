using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

using static MWB.Networking.Layer2_Protocol.UnitTests.Helpers.ProtocolSessionHelpers;

namespace MWB.Networking.Layer2_Protocol.UnitTests;

public partial class ProtocolSessionTests
{
    /// <summary>
    /// Cross-cutting tests: DrainOutbound semantics, Snapshot accuracy,
    /// null/unknown-kind guards, and mixed request + stream scenarios.
    /// </summary>
    [TestClass]
    public sealed partial class Session
    {
        public TestContext TestContext
        {
            get;
            set;
        }

        // ---------------------------------------------------------------
        // DrainOutbound semantics
        // ---------------------------------------------------------------

        [TestMethod]
        public void DrainOutbound_IsEmptyAtStart()
        {
            var session = ProtocolSessionHelpers.CreateSession();
            var runtime = session.Runtime;

            var outbound = runtime.DrainOutboundFrames();

            Assert.IsEmpty(outbound);
        }

        [TestMethod]
        public void DrainOutbound_ClearsQueueAfterFirstCall()
        {
            var session = ProtocolSessionHelpers.CreateSession();
            var runtime = session.Runtime;
            var observer = session.Observer;

            IncomingRequest? r1 = null;
            IncomingRequest? r2 = null;

            observer.RequestReceived += (req, payload) =>
            {
                if (req.Context.RequestId == 1) r1 = req;
                if (req.Context.RequestId == 2) r2 = req;
            };

            // First outbound frame
            runtime.ProcessFrame(ProtocolFrames.Request(1, ProtocolFrames.EmptyPayload));
            r1!.Respond(new byte[] { 0x01 });

            var first = runtime.DrainOutboundFrames();
            var second = runtime.DrainOutboundFrames();

            Assert.HasCount(1, first);
            Assert.IsEmpty(second);

            // Prove new outbound frames appear after drain
            runtime.ProcessFrame(ProtocolFrames.Request(2, ProtocolFrames.EmptyPayload));
            r2!.Respond(new byte[] { 0x02 });

            var third = runtime.DrainOutboundFrames();

            Assert.HasCount(1, third);
            Assert.AreEqual((byte)0x02, third[0].Payload.Span[0]);
        }


        [TestMethod]

        public void DrainOutbound_AccumulatesAcrossMultipleOutboundEmits()
        {
            var session = ProtocolSessionHelpers.CreateSession();
            var runtime = session.Runtime;
            var observer = session.Observer;

            IncomingRequest? request1 = null;
            IncomingRequest? request2 = null;

            observer.RequestReceived += (req, payload) =>
            {
                if (req.Context.RequestId == 1)
                {
                    request1 = req;
                }
                else if (req.Context.RequestId == 2)
                {
                    request2 = req;
                }
            };

            runtime.ProcessFrame(ProtocolFrames.Request(1));
            runtime.ProcessFrame(ProtocolFrames.Request(2));

            // Each request produces exactly one response
            request1!.Respond(new byte[] { 0xA1 });
            request2!.Respond(new byte[] { 0xA2 });

            var outbound = runtime.DrainOutboundFrames();

            Assert.HasCount(2, outbound);
            Assert.AreEqual(ProtocolFrameKind.Response, outbound[0].Kind);
            Assert.AreEqual(ProtocolFrameKind.Response, outbound[1].Kind);
        }


        [TestMethod]
        public void DrainOutbound_AfterPartialDrain_ContainsOnlyNewOutboundFrames()
        {
            var session = ProtocolSessionHelpers.CreateSession();
            var runtime = session.Runtime;
            var observer = session.Observer;

            IncomingRequest? r1 = null;
            IncomingRequest? r2 = null;
            IncomingRequest? r3 = null;

            observer.RequestReceived += (req, payload) =>
            {
                if (req.Context.RequestId == 1) r1 = req;
                if (req.Context.RequestId == 2) r2 = req;
                if (req.Context.RequestId == 3) r3 = req;
            };

            // First outbound frame
            runtime.ProcessFrame(ProtocolFrames.Request(1));
            r1!.Respond(new byte[] { 1 });

            // Drain existing outbound frames
            runtime.DrainOutboundFrames();

            // Two new outbound frames
            runtime.ProcessFrame(ProtocolFrames.Request(2));
            runtime.ProcessFrame(ProtocolFrames.Request(3));
            r2!.Respond(new byte[] { 2 });
            r3!.Respond(new byte[] { 3 });

            var outbound = runtime.DrainOutboundFrames();

            Assert.HasCount(2, outbound);
            Assert.AreEqual((byte)2, outbound[0].Payload.Span[0]);
            Assert.AreEqual((byte)3, outbound[1].Payload.Span[0]);
        }

        // ---------------------------------------------------------------
        // Snapshot accuracy
        // ---------------------------------------------------------------

        [TestMethod]
        public void Snapshot_InitiallyEmpty()
        {
            var session = ProtocolSessionHelpers.CreateSession();
            var runtime = session.Runtime;
            var observer = session.Observer;

            var snap = observer.GetSnapshot();

            Assert.IsEmpty(snap.OpenRequests);
            Assert.IsEmpty(snap.OpenStreams);
        }

        [TestMethod]
        public void Snapshot_IsNonDestructive()
        {
            var session = ProtocolSessionHelpers.CreateSession();
            var runtime = session.Runtime;
            var observer = session.Observer;

            runtime.ProcessFrame(ProtocolFrames.Request(1));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(10));

            var snap1 = observer.GetSnapshot();
            var snap2 = observer.GetSnapshot();

            Assert.HasCount(snap1.OpenRequests.Count, snap2.OpenRequests);
            Assert.HasCount(snap1.OpenStreams.Count, snap2.OpenStreams);
        }

        [TestMethod]
        public void Snapshot_AllClosed_ShowsEmpty()
        {
            var session = ProtocolSessionHelpers.CreateSession();
            var runtime = session.Runtime;
            var observer = session.Observer;

            runtime.ProcessFrame(ProtocolFrames.Request(1));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(10));
            runtime.ProcessFrame(ProtocolFrames.Response(1));
            runtime.ProcessFrame(ProtocolFrames.StreamClose(10));

            var snap = observer.GetSnapshot();

            Assert.IsEmpty(snap.OpenRequests);
            Assert.IsEmpty(snap.OpenStreams);
        }

        [TestMethod]
        public void Snapshot_RequestsAndStreams_AreIndependentIdNamespaces()
        {
            // The same numeric ID may be in use simultaneously as both a request and a stream.
            var session = ProtocolSessionHelpers.CreateSession();
            var runtime = session.Runtime;
            var observer = session.Observer;

            runtime.ProcessFrame(ProtocolFrames.Request(42));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(42));

            var snap = observer.GetSnapshot();

            Assert.Contains(42u, snap.OpenRequests);
            Assert.Contains(42u, snap.OpenStreams);
        }

        [TestMethod]
        public void Snapshot_DoesNotContainStreamIdsInRequestsOrViceVersa()
        {
            var session = ProtocolSessionHelpers.CreateSession();
            var runtime = session.Runtime;
            var observer = session.Observer;

            runtime.ProcessFrame(ProtocolFrames.Request(1));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(2));

            var snap = observer.GetSnapshot();

            Assert.DoesNotContain(2u, snap.OpenRequests);
            Assert.DoesNotContain(1u, snap.OpenStreams);
        }

        // ---------------------------------------------------------------
        // Null / unknown-kind guards
        // ---------------------------------------------------------------

        [TestMethod]
        public void OnInbound_NullFrame_ThrowsArgumentNullException()
        {
            var session = ProtocolSessionHelpers.CreateSession();
            var runtime = session.Runtime;

            Assert.Throws<ArgumentNullException>(() => runtime.ProcessFrame(null!));
        }

        [TestMethod]
        public void OnInbound_UnknownFrameKind_ThrowsProtocolException_WithUnknownFrameKindError()
        {
            var session = ProtocolSessionHelpers.CreateSession();
            var runtime = session.Runtime;

            var frame = new ProtocolFrame((ProtocolFrameKind)0xFF, null, null, null, ProtocolFrames.EmptyPayload);

            var ex = Assert.Throws<ProtocolException>(() => runtime.ProcessFrame(frame));

            Assert.AreEqual(ProtocolErrorKind.UnknownFrameKind, ex.ErrorKind);
        }

        // ---------------------------------------------------------------
        // Mixed request + stream scenarios
        // ---------------------------------------------------------------

        [TestMethod]
        public void MixedRequestsAndStreams_TrackedIndependently()
        {
            var session = ProtocolSessionHelpers.CreateSession();
            var runtime = session.Runtime;
            var observer = session.Observer;

            runtime.ProcessFrame(ProtocolFrames.Request(1));
            runtime.ProcessFrame(ProtocolFrames.Request(2));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(10));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(20));

            var snap = observer.GetSnapshot();

            Assert.HasCount(2, snap.OpenRequests);
            Assert.HasCount(2, snap.OpenStreams);
        }

        [TestMethod]
        public void ClosingRequest_DoesNotAffectOpenStreams()
        {
            var session = ProtocolSessionHelpers.CreateSession();
            var runtime = session.Runtime;
            var observer = session.Observer;

            runtime.ProcessFrame(ProtocolFrames.Request(1));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(10));
            runtime.ProcessFrame(ProtocolFrames.Response(1));

            var snap = observer.GetSnapshot();

            Assert.IsEmpty(snap.OpenRequests);
            Assert.HasCount(1, snap.OpenStreams);
            Assert.Contains(10u, snap.OpenStreams);
        }

        [TestMethod]
        public void ClosingStream_DoesNotAffectOpenRequests()
        {
            var session = ProtocolSessionHelpers.CreateSession();
            var runtime = session.Runtime;
            var observer = session.Observer;

            runtime.ProcessFrame(ProtocolFrames.Request(1));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(10));
            runtime.ProcessFrame(ProtocolFrames.StreamClose(10));

            var snap = observer.GetSnapshot();

            Assert.HasCount(1, snap.OpenRequests);
            Assert.Contains(1u, snap.OpenRequests);
            Assert.IsEmpty(snap.OpenStreams);
        }

        [TestMethod]
        public void Inbound_ResponseFrame_IsAccepted()
        {
            var session = ProtocolSessionHelpers.CreateSession();
            var runtime = session.Runtime;

            runtime.ProcessFrame(ProtocolFrames.Request(1));
            runtime.DrainOutboundFrames();

            // Should not throw
            runtime.ProcessFrame(ProtocolFrames.Response(1, new byte[] { 0x01 }));

            var outbound = runtime.DrainOutboundFrames();
            Assert.HasCount(0, outbound);
        }


        [TestMethod]
        public void FullMixedLifecycle_SnapshotAccurateAtEachStep()
        {
            var session = ProtocolSessionHelpers.CreateSession();
            var runtime = session.Runtime;
            var observer = session.Observer;

            // Open two requests and two streams.
            runtime.ProcessFrame(ProtocolFrames.Request(1));
            runtime.ProcessFrame(ProtocolFrames.Request(2));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(10));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(20));
            Assert.HasCount(2, observer.GetSnapshot().OpenRequests);
            Assert.HasCount(2, observer.GetSnapshot().OpenStreams);

            // Close one request and one stream.
            runtime.ProcessFrame(ProtocolFrames.Response(1));
            runtime.ProcessFrame(ProtocolFrames.StreamClose(10));
            Assert.HasCount(1, observer.GetSnapshot().OpenRequests);
            Assert.HasCount(1, observer.GetSnapshot().OpenStreams);

            // Close the remaining ones.
            runtime.ProcessFrame(ProtocolFrames.Error(2));
            runtime.ProcessFrame(ProtocolFrames.StreamClose(20));
            Assert.IsEmpty(observer.GetSnapshot().OpenRequests);
            Assert.IsEmpty(observer.GetSnapshot().OpenStreams);
        }
    }
}
