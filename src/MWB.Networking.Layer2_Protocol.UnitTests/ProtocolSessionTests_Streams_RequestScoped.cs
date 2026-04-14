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
    public sealed partial class Streams_RequestScoped
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
        // Streams - Request scoped
        // ---------------------------------------------------------------

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
        public void CannotOpenRequestScopedStream_AfterResponse()
        {
            var session = (ProtocolSession)CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            IncomingRequest? request = null;

            session.RequestReceived += (req, payload) =>
            {
                request = req;
            };

            // Inbound request
            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));

            // Respond to the request (closes it)
            Assert.IsNotNull(request);
            request.Respond(new byte[] { 0xAA });

            // Attempting to open a request-scoped stream after response is invalid
            Assert.Throws<InvalidOperationException>(() =>
            {
                request.OpenRequestStream();
            });
        }
    }
}
