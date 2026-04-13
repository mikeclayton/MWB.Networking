using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests;

namespace MWB.Networking.Layer2_Protocol.UnitTests;

public partial class ProtocolSessionTests
{
    /// <summary>
    /// Tests for the stream lifecycle: StreamOpen → StreamData* → StreamClose | Error | Cancel.
    /// Covers snapshot state, outbound frame emission, and protocol violation guards.
    /// </summary>
    [TestClass]
    public sealed partial class Streams
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

        // ---------------------------------------------------------------
        // Snapshot state
        // ---------------------------------------------------------------

        [TestMethod]
        public void StreamOpen_AppearsInSnapshot()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.StreamOpen(1, Empty));

            Assert.Contains(1u, session.Snapshot().OpenStreams);
        }

        [TestMethod]
        public void StreamData_DoesNotCloseStream()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.StreamOpen(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.StreamData(1, new byte[] { 0xAB }));

            Assert.Contains(1u, session.Snapshot().OpenStreams);
        }

        [TestMethod]
        public void StreamData_MultipleFrames_StreamRemainsOpen()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.StreamOpen(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.StreamData(1, new byte[] { 1 }));
            runtime.ProcessFrame(ProtocolFrames.StreamData(1, new byte[] { 2 }));
            runtime.ProcessFrame(ProtocolFrames.StreamData(1, new byte[] { 3 }));

            Assert.Contains(1u, session.Snapshot().OpenStreams);
        }

        [TestMethod]
        public void StreamClose_RemovesStreamFromSnapshot()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.StreamOpen(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.StreamClose(1));

            Assert.IsEmpty(session.Snapshot().OpenStreams);
        }

        [TestMethod]
        public void MultipleConcurrentStreams_AllTrackedInSnapshot()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.StreamOpen(10, Empty));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(20, Empty));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(30, Empty));

            var snap = session.Snapshot();

            Assert.HasCount(3, snap.OpenStreams);
            Assert.Contains(10u, snap.OpenStreams);
            Assert.Contains(20u, snap.OpenStreams);
            Assert.Contains(30u, snap.OpenStreams);
        }

        [TestMethod]
        public void MultipleStreams_CloseIndependently()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.StreamOpen(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(2, Empty));
            runtime.ProcessFrame(ProtocolFrames.StreamClose(1));

            var snap = session.Snapshot();

            Assert.HasCount(1, snap.OpenStreams);
            Assert.DoesNotContain(1u, snap.OpenStreams);
            Assert.Contains(2u, snap.OpenStreams);
        }

        [TestMethod]
        public void StreamId_ReusableAfterClose()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.StreamOpen(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.StreamClose(1));

            // The same ID may be reused once the previous stream has closed.
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(1, Empty));

            Assert.Contains(1u, session.Snapshot().OpenStreams);
        }

        // ---------------------------------------------------------------
        // Outbound frame emission
        // ---------------------------------------------------------------

        [TestMethod]
        public void StreamOpen_IsEmittedToOutbound()
        {
            var session = (ProtocolSession)CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            var metadata = new byte[] { 0x01, 0x02 };

            // Open a session-scoped stream via intent-level API
            var stream = session.OpenSessionStream(metadata);

            var outbound = runtime.DrainOutboundFrames();

            Assert.HasCount(1, outbound);
            Assert.AreEqual(ProtocolFrameKind.StreamOpen, outbound[0].Kind);
            Assert.AreEqual(stream.StreamId, outbound[0].StreamId);
            CollectionAssert.AreEqual(metadata, outbound[0].Payload.ToArray());
        }

        [TestMethod]
        public void StreamData_IsEmittedToOutbound()
        {
            var session = (ProtocolSession)CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            var data = new byte[] { 0xDE, 0xAD };

            // Open a session-scoped stream
            var stream = session.OpenSessionStream(Empty);

            // Discard StreamOpen
            runtime.DrainOutboundFrames();

            // Send data via intent-level API
            stream.SendData(data);

            var outbound = runtime.DrainOutboundFrames();

            Assert.HasCount(1, outbound);
            Assert.AreEqual(ProtocolFrameKind.StreamData, outbound[0].Kind);
            Assert.AreEqual(stream.StreamId, outbound[0].StreamId);
            CollectionAssert.AreEqual(data, outbound[0].Payload.ToArray());
        }

        [TestMethod]
        public void InboundStreamData_DoesNotEmitOutbound()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.StreamOpen(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.StreamData(1, new byte[] { 10 }));
            runtime.ProcessFrame(ProtocolFrames.StreamData(1, new byte[] { 20 }));

            Assert.IsEmpty(runtime.DrainOutboundFrames());
        }

        [TestMethod]
        public void MultipleStreamData_EmittedInOrder()
        {
            var session = (ProtocolSession)CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            // Open a session-scoped stream
            var stream = session.OpenSessionStream(Empty);

            // Emit multiple data frames
            stream.SendData(new byte[] { 10 });
            stream.SendData(new byte[] { 20 });
            stream.SendData(new byte[] { 30 });

            var outbound = runtime.DrainOutboundFrames();

            Assert.AreEqual(4, outbound.Count);

            // First frame is StreamOpen
            Assert.AreEqual(ProtocolFrameKind.StreamOpen, outbound[0].Kind);

            // StreamData frames in order
            Assert.AreEqual((byte)10, outbound[1].Payload.Span[0]);
            Assert.AreEqual((byte)20, outbound[2].Payload.Span[0]);
            Assert.AreEqual((byte)30, outbound[3].Payload.Span[0]);
        }

        [TestMethod]
        public void StreamClose_IsEmittedToOutbound()
        {
            var session = (ProtocolSession)CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            // Open a session-scoped stream
            var stream = session.OpenSessionStream(Empty);

            // Discard the StreamOpen frame
            runtime.DrainOutboundFrames();

            // Close the stream via intent-level API
            stream.Close();

            var outbound = runtime.DrainOutboundFrames();

            Assert.HasCount(1, outbound);
            Assert.AreEqual(ProtocolFrameKind.StreamClose, outbound[0].Kind);
            Assert.AreEqual(stream.StreamId, outbound[0].StreamId);
        }

        [TestMethod]
        public void FullRequestScopedStreamLifecycle_AllFramesEmittedInOrder()
        {
            var session = (ProtocolSession)CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            IncomingRequest? request = null;

            session.RequestReceived += (req, _) => request = req;

            // Inbound request
            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.DrainOutboundFrames();

            Assert.IsNotNull(request);

            // Open request-scoped stream
            var stream = request.OpenRequestStream();

            stream.SendData(new byte[] { 0xA1 });
            stream.SendData(new byte[] { 0xA2 });
            stream.Close();

            var outbound = runtime.DrainOutboundFrames();

            Assert.HasCount(4, outbound);
            Assert.AreEqual(ProtocolFrameKind.StreamOpen, outbound[0].Kind);
            Assert.AreEqual(ProtocolFrameKind.StreamData, outbound[1].Kind);
            Assert.AreEqual(ProtocolFrameKind.StreamData, outbound[2].Kind);
            Assert.AreEqual(ProtocolFrameKind.StreamClose, outbound[3].Kind);
        }

        [TestMethod]
        public void FullSessionScopedStreamLifecycle_AllFramesEmittedInOrder()
        {
            var session = (ProtocolSession)CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            // Open a session-scoped stream
            var stream = session.OpenSessionStream(new byte[] { 0xF0 });

            // Send data and close
            stream.SendData(new byte[] { 0xA1 });
            stream.SendData(new byte[] { 0xA2 });
            stream.Close();

            var outbound = runtime.DrainOutboundFrames();

            Assert.HasCount(4, outbound);

            Assert.AreEqual(ProtocolFrameKind.StreamOpen, outbound[0].Kind);
            Assert.AreEqual(ProtocolFrameKind.StreamData, outbound[1].Kind);
            Assert.AreEqual(ProtocolFrameKind.StreamData, outbound[2].Kind);
            Assert.AreEqual(ProtocolFrameKind.StreamClose, outbound[3].Kind);
        }

        [TestMethod]
        public void InterleavedSessionStreams_FramesEmittedInOrder()
        {
            var session = (ProtocolSession)CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            // Open two session-scoped streams
            var stream1 = session.OpenSessionStream(Empty);
            var stream2 = session.OpenSessionStream(Empty);

            // Interleave data writes
            stream1.SendData(new byte[] { 0x01 });
            stream2.SendData(new byte[] { 0x02 });

            // Close in the same interleaved order
            stream1.Close();
            stream2.Close();

            var outbound = runtime.DrainOutboundFrames();

            Assert.HasCount(6, outbound);

            // Open frames
            Assert.AreEqual(ProtocolFrameKind.StreamOpen, outbound[0].Kind);
            Assert.AreEqual(ProtocolFrameKind.StreamOpen, outbound[1].Kind);

            // Interleaved data
            Assert.AreEqual(ProtocolFrameKind.StreamData, outbound[2].Kind);
            Assert.AreEqual(ProtocolFrameKind.StreamData, outbound[3].Kind);

            // Close frames
            Assert.AreEqual(ProtocolFrameKind.StreamClose, outbound[4].Kind);
            Assert.AreEqual(ProtocolFrameKind.StreamClose, outbound[5].Kind);
        }

        // ---------------------------------------------------------------
        // Protocol violations
        // ---------------------------------------------------------------

        [TestMethod]
        public void StreamOpen_MissingStreamId_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            var frame = new ProtocolFrame(ProtocolFrameKind.StreamOpen, null, null, null, Empty);

            Assert.Throws<ProtocolException>(() => runtime.ProcessFrame(frame));
        }

        [TestMethod]
        public void DuplicateStreamId_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.StreamOpen(1, Empty));

            Assert.Throws<ProtocolException>(
                () => runtime.ProcessFrame(ProtocolFrames.StreamOpen(1, Empty)));
        }

        [TestMethod]
        public void StreamData_UnknownStreamId_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            Assert.Throws<ProtocolException>(
                () => runtime.ProcessFrame(ProtocolFrames.StreamData(99, new byte[] { 1 })));
        }

        [TestMethod]
        public void StreamClose_UnknownStreamId_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            Assert.Throws<ProtocolException>(
                () => runtime.ProcessFrame(ProtocolFrames.StreamClose(99)));
        }

        [TestMethod]
        public void StreamData_MissingStreamId_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            var frame = new ProtocolFrame(ProtocolFrameKind.StreamData, null, null, null, Empty);

            Assert.Throws<ProtocolException>(() => runtime.ProcessFrame(frame));
        }

        [TestMethod]
        public void StreamData_AfterClose_ThrowsProtocolException()
        {
            // StreamClose removes the context; the same StreamId is now unknown.
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.StreamOpen(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.StreamClose(1));

            Assert.Throws<ProtocolException>(
                () => runtime.ProcessFrame(ProtocolFrames.StreamData(1, new byte[] { 1 })));
        }

        [TestMethod]
        public void StreamClose_Twice_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.StreamOpen(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.StreamClose(1));

            Assert.Throws<ProtocolException>(
                () => runtime.ProcessFrame(ProtocolFrames.StreamClose(1)));
        }


        [TestMethod]
        public void InboundStreamFrames_DoNotEmitOutbound()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.StreamOpen(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.StreamData(1, new byte[] { 0x01 }));
            runtime.ProcessFrame(ProtocolFrames.StreamClose(1));

            Assert.IsEmpty(runtime.DrainOutboundFrames());
        }

        [TestMethod]
        public void LocalSessionStreams_MayInterleaveOutboundFrames()
        {
            var session = (ProtocolSession)CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            var s1 = session.OpenSessionStream(Empty);
            var s2 = session.OpenSessionStream(Empty);

            s1.SendData(new byte[] { 0x01 });
            s2.SendData(new byte[] { 0x02 });
            s1.SendData(new byte[] { 0x03 });
            s2.SendData(new byte[] { 0x04 });

            s1.Close();
            s2.Close();

            var outbound = runtime.DrainOutboundFrames();

            Assert.AreEqual(8, outbound.Count);

            // Open frames
            Assert.AreEqual(ProtocolFrameKind.StreamOpen, outbound[0].Kind);
            Assert.AreEqual(ProtocolFrameKind.StreamOpen, outbound[1].Kind);

            var id1 = outbound[0].StreamId;
            var id2 = outbound[1].StreamId;

            // Interleaved data
            Assert.AreEqual(id1, outbound[2].StreamId);
            Assert.AreEqual(id2, outbound[3].StreamId);
            Assert.AreEqual(id1, outbound[4].StreamId);
            Assert.AreEqual(id2, outbound[5].StreamId);

            // Close frames
            Assert.AreEqual(ProtocolFrameKind.StreamClose, outbound[6].Kind);
            Assert.AreEqual(id1, outbound[6].StreamId);
            Assert.AreEqual(ProtocolFrameKind.StreamClose, outbound[7].Kind);
            Assert.AreEqual(id2, outbound[7].StreamId);
        }
    }
}
