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
    }
}
