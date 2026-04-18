using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

namespace MWB.Networking.Layer2_Protocol.UnitTests;

public partial class ProtocolSessionTests
{
    /// <summary>
    /// Tests for the request lifecycle: Request → Response* → Complete | Error | Cancel.
    /// Covers snapshot state, outbound frame emission, and protocol violation guards.
    /// </summary>
    [TestClass]
    public sealed partial class Streams_Inbound
    {
        public TestContext TestContext
        {
            get;
            set;
        }

        // ---------------------------------------------------------------
        // Streams - Inbound
        // ---------------------------------------------------------------

        [TestMethod]
        public void StreamOpen_AppearsInSnapshot()
        {
            var session = ProtocolSessionHelper.CreateNullSession();
            var runtime = session.Runtime;

            runtime.ProcessFrame(ProtocolFrames.StreamOpen(1));

            Assert.Contains(1u, session.Diagnostics.GetSnapshot().OpenStreams);
        }

        [TestMethod]
        public void StreamData_DoesNotCloseStream()
        {
            var session = ProtocolSessionHelper.CreateNullSession();
            var runtime = session.Runtime;

            runtime.ProcessFrame(ProtocolFrames.StreamOpen(1));
            runtime.ProcessFrame(ProtocolFrames.StreamData(1, new byte[] { 0xAB }));

            Assert.Contains(1u, session.Diagnostics.GetSnapshot().OpenStreams);
        }

        [TestMethod]
        public void StreamData_MultipleFrames_StreamRemainsOpen()
        {
            var session = ProtocolSessionHelper.CreateNullSession();
            var runtime = session.Runtime;

            runtime.ProcessFrame(ProtocolFrames.StreamOpen(1));
            runtime.ProcessFrame(ProtocolFrames.StreamData(1, new byte[] { 1 }));
            runtime.ProcessFrame(ProtocolFrames.StreamData(1, new byte[] { 2 }));
            runtime.ProcessFrame(ProtocolFrames.StreamData(1, new byte[] { 3 }));

            Assert.Contains(1u, session.Diagnostics.GetSnapshot().OpenStreams);
        }

        [TestMethod]
        public void StreamClose_RemovesStreamFromSnapshot()
        {
            var session = ProtocolSessionHelper.CreateNullSession();
            var runtime = session.Runtime;

            runtime.ProcessFrame(ProtocolFrames.StreamOpen(1));
            runtime.ProcessFrame(ProtocolFrames.StreamClose(1));

            Assert.IsEmpty(session.Diagnostics.GetSnapshot().OpenStreams);
        }

        [TestMethod]
        public void MultipleConcurrentStreams_AllTrackedInSnapshot()
        {
            var session = ProtocolSessionHelper.CreateNullSession();
            var runtime = session.Runtime;

            runtime.ProcessFrame(ProtocolFrames.StreamOpen(10));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(20));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(30));

            var snap = session.Diagnostics.GetSnapshot();

            Assert.HasCount(3, snap.OpenStreams);
            Assert.Contains(10u, snap.OpenStreams);
            Assert.Contains(20u, snap.OpenStreams);
            Assert.Contains(30u, snap.OpenStreams);
        }

        [TestMethod]
        public void MultipleStreams_CloseIndependently()
        {
            var session = ProtocolSessionHelper.CreateNullSession();
            var runtime = session.Runtime;

            runtime.ProcessFrame(ProtocolFrames.StreamOpen(1));
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(2));
            runtime.ProcessFrame(ProtocolFrames.StreamClose(1));

            var snap = session.Diagnostics.GetSnapshot();

            Assert.HasCount(1, snap.OpenStreams);
            Assert.DoesNotContain(1u, snap.OpenStreams);
            Assert.Contains(2u, snap.OpenStreams);
        }

        [TestMethod]
        public void StreamId_ReusableAfterClose()
        {
            var session = ProtocolSessionHelper.CreateNullSession();
            var runtime = session.Runtime;

            runtime.ProcessFrame(ProtocolFrames.StreamOpen(1));
            runtime.ProcessFrame(ProtocolFrames.StreamClose(1));

            // The same ID may be reused once the previous stream has closed.
            runtime.ProcessFrame(ProtocolFrames.StreamOpen(1));

            Assert.Contains(1u, session.Diagnostics.GetSnapshot().OpenStreams);
        }

        [TestMethod]
        public void InboundStreamData_DoesNotEmitOutbound()
        {
            var session = ProtocolSessionHelper.CreateNullSession();
            var runtime = session.Runtime;

            runtime.ProcessFrame(ProtocolFrames.StreamOpen(1));
            runtime.ProcessFrame(ProtocolFrames.StreamData(1, new byte[] { 10 }));
            runtime.ProcessFrame(ProtocolFrames.StreamData(1, new byte[] { 20 }));

            Assert.IsEmpty(runtime.DrainOutboundFrames());
        }


        [TestMethod]
        public void InboundStreamFrames_DoNotEmitOutbound()
        {
            var session = ProtocolSessionHelper.CreateNullSession();
            var runtime = session.Runtime;

            runtime.ProcessFrame(ProtocolFrames.StreamOpen(1));
            runtime.ProcessFrame(ProtocolFrames.StreamData(1, new byte[] { 0x01 }));
            runtime.ProcessFrame(ProtocolFrames.StreamClose(1));

            Assert.IsEmpty(runtime.DrainOutboundFrames());
        }
    }
}
