using MWB.Networking.Layer2_Protocol;
using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests;

namespace MWB.Networking.Layer2_Protocol.UnitTests;

public partial class ProtocolSessionTests
{
    /// <summary>
    /// Cross-cutting tests: DrainOutbound semantics, Snapshot accuracy,
    /// null/unknown-kind guards, and mixed request + stream scenarios.
    /// </summary>
    [TestClass]
    public sealed class Session
    {
        public TestContext TestContext
        {
            get;
            set;
        }

#pragma warning disable CA1859 // Use concrete types when possible for improved performance
        private static IProtocolSession CreateSession() => new ProtocolSession();
#pragma warning restore CA1859 // Use concrete types when possible for improved performance

        private static readonly ReadOnlyMemory<byte> Empty = ReadOnlyMemory<byte>.Empty;

        private static ProtocolFrame MakeError(uint requestId) =>
            new(ProtocolFrameKind.Error, null, requestId, null, ReadOnlyMemory<byte>.Empty);

        // ---------------------------------------------------------------
        // DrainOutbound semantics
        // ---------------------------------------------------------------

        [TestMethod]
        public void DrainOutbound_IsEmptyAtStart()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            var outbound = runtime.DrainOutboundFrames();

            Assert.IsEmpty(outbound);
        }

        [TestMethod]
        public void DrainOutbound_ClearsQueueAfterFirstCall()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            IncomingRequest? r1 = null;
            IncomingRequest? r2 = null;

            session.RequestReceived += (req, payload) =>
            {
                if (req.RequestId == 1) r1 = req;
                if (req.RequestId == 2) r2 = req;
            };

            // First outbound frame
            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            r1!.Respond(new byte[] { 0x01 });

            var first = runtime.DrainOutboundFrames();
            var second = runtime.DrainOutboundFrames();

            Assert.HasCount(1, first);
            Assert.IsEmpty(second);

            // Prove new outbound frames appear after drain
            runtime.ProcessFrame(ProtocolFrames.Request(2, Empty));
            r2!.Respond(new byte[] { 0x02 });

            var third = runtime.DrainOutboundFrames();

            Assert.HasCount(1, third);
            Assert.AreEqual((byte)0x02, third[0].Payload.Span[0]);
        }


        [TestMethod]

        public void DrainOutbound_AccumulatesAcrossMultipleOutboundEmits()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            IncomingRequest? request1 = null;
            IncomingRequest? request2 = null;

            session.RequestReceived += (req, payload) =>
            {
                if (req.RequestId == 1)
                {
                    request1 = req;
                }
                else if (req.RequestId == 2)
                {
                    request2 = req;
                }
            };

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.Request(2, Empty));

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
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            IncomingRequest? r1 = null;
            IncomingRequest? r2 = null;
            IncomingRequest? r3 = null;

            session.RequestReceived += (req, payload) =>
            {
                if (req.RequestId == 1) r1 = req;
                if (req.RequestId == 2) r2 = req;
                if (req.RequestId == 3) r3 = req;
            };

            // First outbound frame
            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            r1!.Respond(new byte[] { 1 });

            // Drain existing outbound frames
            runtime.DrainOutboundFrames();

            // Two new outbound frames
            runtime.ProcessFrame(ProtocolFrames.Request(2, Empty));
            runtime.ProcessFrame(ProtocolFrames.Request(3, Empty));
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
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            var snap = session.Snapshot();

            Assert.IsEmpty(snap.OpenRequests);
            Assert.IsEmpty(snap.OpenStreams);
        }

        [TestMethod]
        public void Snapshot_IsNonDestructive()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(10, Empty));

            var snap1 = session.Snapshot();
            var snap2 = session.Snapshot();

            Assert.HasCount(snap1.OpenRequests.Count, snap2.OpenRequests);
            Assert.HasCount(snap1.OpenStreams.Count, snap2.OpenStreams);
        }

        [TestMethod]
        public void Snapshot_AllClosed_ShowsEmpty()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(10, Empty));
            runtime.ProcessFrame(ProtocolFrames.CompleteRequest(1));
            runtime.ProcessFrame(ProtocolFrames.StreamClose(10));

            var snap = session.Snapshot();

            Assert.IsEmpty(snap.OpenRequests);
            Assert.IsEmpty(snap.OpenStreams);
        }

        [TestMethod]
        public void Snapshot_RequestsAndStreams_AreIndependentIdNamespaces()
        {
            // The same numeric ID may be in use simultaneously as both a request and a stream.
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(42, Empty));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(42, Empty));

            var snap = session.Snapshot();

            Assert.Contains(42u, snap.OpenRequests);
            Assert.Contains(42u, snap.OpenStreams);
        }

        [TestMethod]
        public void Snapshot_DoesNotContainStreamIdsInRequestsOrViceVersa()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(2, Empty));

            var snap = session.Snapshot();

            Assert.DoesNotContain(2u, snap.OpenRequests);
            Assert.DoesNotContain(1u, snap.OpenStreams);
        }

        // ---------------------------------------------------------------
        // Null / unknown-kind guards
        // ---------------------------------------------------------------

        [TestMethod]
        public void OnInbound_NullFrame_ThrowsArgumentNullException()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            Assert.Throws<ArgumentNullException>(() => runtime.ProcessFrame(null!));
        }

        [TestMethod]
        public void OnInbound_UnknownFrameKind_ThrowsProtocolException_WithUnknownFrameKindError()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            var frame = new ProtocolFrame((ProtocolFrameKind)0xFF, null, null, null, Empty);

            var ex = Assert.Throws<ProtocolException>(() => runtime.ProcessFrame(frame));

            Assert.AreEqual(ProtocolErrorKind.UnknownFrameKind, ex.ErrorKind);
        }

        // ---------------------------------------------------------------
        // Mixed request + stream scenarios
        // ---------------------------------------------------------------

        [TestMethod]
        public void MixedRequestsAndStreams_TrackedIndependently()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.Request(2, Empty));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(10, Empty));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(20, Empty));

            var snap = session.Snapshot();

            Assert.HasCount(2, snap.OpenRequests);
            Assert.HasCount(2, snap.OpenStreams);
        }

        [TestMethod]
        public void ClosingRequest_DoesNotAffectOpenStreams()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(10, Empty));
            runtime.ProcessFrame(ProtocolFrames.CompleteRequest(1));

            var snap = session.Snapshot();

            Assert.IsEmpty(snap.OpenRequests);
            Assert.HasCount(1, snap.OpenStreams);
            Assert.Contains(10u, snap.OpenStreams);
        }

        [TestMethod]
        public void ClosingStream_DoesNotAffectOpenRequests()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(10, Empty));
            runtime.ProcessFrame(ProtocolFrames.StreamClose(10));

            var snap = session.Snapshot();

            Assert.HasCount(1, snap.OpenRequests);
            Assert.Contains(1u, snap.OpenRequests);
            Assert.IsEmpty(snap.OpenStreams);
        }

        [TestMethod]
        public void Inbound_ResponseFrame_IsAlwaysProtocolViolation()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));

            var ex = Assert.Throws<ProtocolException>(() =>
                runtime.ProcessFrame(ProtocolFrames.Response(1, new byte[] { 0x01 }))
            );

            Assert.AreEqual(ProtocolErrorKind.InvalidFrameSequence, ex.ErrorKind);
        }


        [TestMethod]
        public void FullMixedLifecycle_SnapshotAccurateAtEachStep()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            // Open two requests and two streams.
            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.Request(2, Empty));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(10, Empty));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(20, Empty));
            Assert.HasCount(2, session.Snapshot().OpenRequests);
            Assert.HasCount(2, session.Snapshot().OpenStreams);

            // Close one request and one stream.
            runtime.ProcessFrame(ProtocolFrames.CompleteRequest(1));
            runtime.ProcessFrame(ProtocolFrames.StreamClose(10));
            Assert.HasCount(1, session.Snapshot().OpenRequests);
            Assert.HasCount(1, session.Snapshot().OpenStreams);

            // Close the remaining ones.
            runtime.ProcessFrame(MakeError(2));
            runtime.ProcessFrame(ProtocolFrames.StreamClose(20));
            Assert.IsEmpty(session.Snapshot().OpenRequests);
            Assert.IsEmpty(session.Snapshot().OpenStreams);
        }
    }
}
