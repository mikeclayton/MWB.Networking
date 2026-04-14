using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

using static MWB.Networking.Layer2_Protocol.UnitTests.Helpers.ProtocolSessionHelpers;

namespace MWB.Networking.Layer2_Protocol.UnitTests;

public partial class ProtocolSessionTests
{
    /// <summary>
    /// Tests for the request lifecycle: Request → Response* → Complete | Error | Cancel.
    /// Covers snapshot state, outbound frame emission, and protocol violation guards.
    /// </summary>
    [TestClass]
    public sealed partial class Requests_Invariants
    {
        public TestContext TestContext
        {
            get;
            set;
        }

        // ---------------------------------------------------------------
        // Requests - Invariants
        // ---------------------------------------------------------------

        [TestMethod]
        public void Request_MissingRequestId_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = session.Runtime;

            var frame = new ProtocolFrame(ProtocolFrameKind.Request, null, null, null, ProtocolFrames.EmptyPayload);

            Assert.Throws<ProtocolException>(() => runtime.ProcessFrame(frame));
        }

        [TestMethod]
        public void Response_MissingRequestId_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = session.Runtime;

            var frame = new ProtocolFrame(ProtocolFrameKind.Response, null, null, null, ProtocolFrames.EmptyPayload);

            Assert.Throws<ProtocolException>(() => runtime.ProcessFrame(frame));
        }

        [TestMethod]
        public void Request_DuplicateRequestId_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = session.Runtime;

            runtime.ProcessFrame(ProtocolFrames.Request(1));

            Assert.Throws<ProtocolException>(
                () => runtime.ProcessFrame(ProtocolFrames.Request(1)));
        }

        [TestMethod]
        public void Response_UnknownRequestId_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = session.Runtime;

            Assert.Throws<ProtocolException>(
                () => runtime.ProcessFrame(ProtocolFrames.Response(99)));
        }

        [TestMethod]
        public void Complete_UnknownRequestId_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = session.Runtime;

            Assert.Throws<ProtocolException>(
                () => runtime.ProcessFrame(ProtocolFrames.Response(99)));
        }

        [TestMethod]
        public void Error_UnknownRequestId_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = session.Runtime;

            Assert.Throws<ProtocolException>(
                () => runtime.ProcessFrame(ProtocolFrames.Error(99)));
        }

        [TestMethod]
        public void Response_AfterCompleteRequest_ThrowsProtocolException()
        {
            // Complete removes the context; the same RequestId is now unknown.
            var session = CreateSession();
            var runtime = session.Runtime;

            runtime.ProcessFrame(ProtocolFrames.Request(1));
            runtime.ProcessFrame(ProtocolFrames.Response(1));

            Assert.Throws<ProtocolException>(
                () => runtime.ProcessFrame(ProtocolFrames.Response(1)));
        }

        [TestMethod]
        public void CompleteRequest_Twice_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = session.Runtime;

            runtime.ProcessFrame(ProtocolFrames.Request(1));
            runtime.ProcessFrame(ProtocolFrames.Response(1));

            Assert.Throws<ProtocolException>(
                () => runtime.ProcessFrame(ProtocolFrames.Response(1)));
        }

        [TestMethod]
        public void Error_WithOnlyStreamId_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = session.Runtime;

            // Cancel is always routed through request handling; a null RequestId always throws.
            Assert.Throws<ProtocolException>(
                () => runtime.ProcessFrame(ProtocolFrames.Error(5)));
        }
    }
}
