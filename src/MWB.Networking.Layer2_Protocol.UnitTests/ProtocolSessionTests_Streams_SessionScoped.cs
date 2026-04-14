using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Requests;

namespace MWB.Networking.Layer2_Protocol.UnitTests;

public partial class ProtocolSessionTests
{
    /// <summary>
    /// Tests for the request lifecycle: Request → Response* → Complete | Error | Cancel.
    /// Covers snapshot state, outbound frame emission, and protocol violation guards.
    /// </summary>
    [TestClass]
    public sealed partial class Streams_SessionScoped
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
        // Streams - Session scoped
        // ---------------------------------------------------------------

        [TestMethod]
        public void StreamsMayBeOpenedIndependentlyOfRequests()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            IncomingRequest? request = null;

            session.RequestReceived += (req, payload) =>
            {
                request = req;
            };

            // Open a request
            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));

            // Respond, closing the request
            Assert.IsNotNull(request);
            request.Respond(new byte[] { 0xAA });

            // Open an independent stream (inbound)
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(10, Empty));

            var snapshot = session.Snapshot();

            // Request is closed
            Assert.DoesNotContain(1u, snapshot.OpenRequests);

            // Independent stream exists
            Assert.Contains(10u, snapshot.OpenStreams);
        }

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
    }
}
