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
