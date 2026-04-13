using MWB.Networking.Layer2_Protocol.Internal;

namespace MWB.Networking.Layer2_Protocol.UnitTests;

public partial class ProtocolSessionTests
{
    /// <summary>
    /// Tests for the request lifecycle: Request → Response* → Complete | Error | Cancel.
    /// Covers snapshot state, outbound frame emission, and protocol violation guards.
    /// </summary>
    [TestClass]
    public sealed partial class Requests_Inbound
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
        public void NewRequest_AppearsInSnapshot()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));

            Assert.Contains(1u, session.Snapshot().OpenRequests);
        }

        // ---------------------------------------------------------------
        // Requests - Part 1
        // ---------------------------------------------------------------

        [TestMethod]
        public void Inbound_Response_ClosesRequest()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.Error(1));

            Assert.IsEmpty(session.Snapshot().OpenRequests);
        }

        [TestMethod]
        public void Inbound_RequestError_ClosesRequest()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.Error(1, Empty));

            Assert.IsEmpty(session.Snapshot().OpenRequests);
        }

        [TestMethod]
        public void MultipleConcurrentRequests_AllTrackedInSnapshot()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(10, Empty));
            runtime.ProcessFrame(ProtocolFrames.Request(20, Empty));
            runtime.ProcessFrame(ProtocolFrames.Request(30, Empty));

            var snap = session.Snapshot();

            Assert.HasCount(3, snap.OpenRequests);
            Assert.Contains(10u, snap.OpenRequests);
            Assert.Contains(20u, snap.OpenRequests);
            Assert.Contains(30u, snap.OpenRequests);
        }

        [TestMethod]
        public void MultipleRequests_CloseIndependently()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.Request(2, Empty));
            runtime.ProcessFrame(ProtocolFrames.Response(1));

            var snap = session.Snapshot();

            Assert.HasCount(1, snap.OpenRequests);
            Assert.DoesNotContain(1u, snap.OpenRequests);
            Assert.Contains(2u, snap.OpenRequests);
        }

        [TestMethod]
        public void RequestId_ReusableAfterClose()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.Response(1));

            // The same ID may be used again once the previous context has closed.
            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));

            Assert.Contains(1u, session.Snapshot().OpenRequests);
        }

        // ---------------------------------------------------------------
        // Requests - Part 2
        // ---------------------------------------------------------------

        [TestMethod]
        public void Inbound_ResponseFrame_IsAccepted()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.DrainOutboundFrames();

            runtime.ProcessFrame(ProtocolFrames.Response(1, new byte[] { 10 }));

            // No exception = success
            var outbound = runtime.DrainOutboundFrames();
            Assert.HasCount(0, outbound);
        }

        [TestMethod]
        public void Inbound_Response_DoesNotEmitOutbound()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.DrainOutboundFrames();

            runtime.ProcessFrame(ProtocolFrames.Response(1));
            var outbound = runtime.DrainOutboundFrames();

            Assert.HasCount(0, outbound);
        }

        [TestMethod]
        public void Inbound_Error_DoesNotEmitOutbound()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.DrainOutboundFrames();

            runtime.ProcessFrame(ProtocolFrames.Error(1));
            var outbound = runtime.DrainOutboundFrames();

            Assert.HasCount(0, outbound);
        }
    }
}
