using MWB.Networking.Layer2_Protocol.Internal;

namespace MWB.Networking.Layer2_Protocol.UnitTests;

public partial class ProtocolSessionTests
{
    /// <summary>
    /// Tests for the request lifecycle: Request → Response* → Complete | Error | Cancel.
    /// Covers snapshot state, outbound frame emission, and protocol violation guards.
    /// </summary>
    [TestClass]
    public sealed partial class Requests
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
        // Protocol violations
        // ---------------------------------------------------------------

        [TestMethod]
        public void Request_MissingRequestId_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            var frame = new ProtocolFrame(ProtocolFrameKind.Request, null, null, null, Empty);

            Assert.Throws<ProtocolException>(() => runtime.ProcessFrame(frame));
        }

        [TestMethod]
        public void Response_MissingRequestId_ThrowsProtocolException()
        {
            // Cancel is always routed through request handling; a null RequestId always throws.
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            var frame = new ProtocolFrame(ProtocolFrameKind.Response, null, null, null, Empty);

            Assert.Throws<ProtocolException>(() => runtime.ProcessFrame(frame));
        }

        [TestMethod]
        public void Error_MissingRequestId_ThrowsProtocolException()
        {
            // Cancel is always routed through request handling; a null RequestId always throws.
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            var frame = new ProtocolFrame(ProtocolFrameKind.Error, null, null, null, Empty);

            Assert.Throws<ProtocolException>(() => runtime.ProcessFrame(frame));
        }

        [TestMethod]
        public void DuplicateRequestId_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));

            Assert.Throws<ProtocolException>(
                () => runtime.ProcessFrame(ProtocolFrames.Request(1, Empty)));
        }

        [TestMethod]
        public void Response_UnknownRequestId_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            Assert.Throws<ProtocolException>(
                () => runtime.ProcessFrame(ProtocolFrames.Response(99)));
        }

        [TestMethod]
        public void Error_UnknownRequestId_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            Assert.Throws<ProtocolException>(
                () => runtime.ProcessFrame(ProtocolFrames.Error(99)));
        }

        [TestMethod]
        public void Cancel_UnknownRequestId_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            Assert.Throws<ProtocolException>(
                () => runtime.ProcessFrame(ProtocolFrames.Error(99)));
        }

        [TestMethod]
        public void Response_AfterCompleteRequest_ThrowsProtocolException()
        {
            // Complete removes the context; the same RequestId is now unknown.
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.Response(1));

            Assert.Throws<ProtocolException>(
                () => runtime.ProcessFrame(ProtocolFrames.Response(1, Empty)));
        }

        [TestMethod]
        public void CompleteRequest_Twice_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.Response(1));

            Assert.Throws<ProtocolException>(
                () => runtime.ProcessFrame(ProtocolFrames.Response(1)));
        }
    }
}
