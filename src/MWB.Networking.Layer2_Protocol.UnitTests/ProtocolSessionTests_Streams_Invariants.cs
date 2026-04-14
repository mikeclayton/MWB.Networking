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
    public sealed partial class Streams_Invariants
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
        // Streams - Invariants
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
    }
}
