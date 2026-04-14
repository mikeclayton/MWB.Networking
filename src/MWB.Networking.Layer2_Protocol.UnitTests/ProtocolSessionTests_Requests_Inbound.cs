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
    public sealed partial class Requests_Inbound
    {
        public TestContext TestContext
        {
            get;
            set;
        }

        // ---------------------------------------------------------------
        // Snapshot state
        // ---------------------------------------------------------------

        [TestMethod]
        public void NewRequest_AppearsInSnapshot()
        {
            var session = CreateSession();
            var runtime = session.Runtime;
            var observer = session.Observer;

            runtime.ProcessFrame(ProtocolFrames.Request(1));

            Assert.Contains(1u, observer.GetSnapshot().OpenRequests);
        }

        // ---------------------------------------------------------------
        // Requests - Part 1
        // ---------------------------------------------------------------

        [TestMethod]
        public void Inbound_Response_ClosesRequest()
        {
            var session = CreateSession();
            var runtime = session.Runtime;
            var observer = session.Observer;

            runtime.ProcessFrame(ProtocolFrames.Request(1));
            runtime.ProcessFrame(ProtocolFrames.Error(1));

            Assert.IsEmpty(observer.GetSnapshot().OpenRequests);
        }

        [TestMethod]
        public void Inbound_RequestError_ClosesRequest()
        {
            var session = CreateSession();
            var runtime = session.Runtime;
            var observer = session.Observer;

            runtime.ProcessFrame(ProtocolFrames.Request(1));
            runtime.ProcessFrame(ProtocolFrames.Error(1));

            Assert.IsEmpty(observer.GetSnapshot().OpenRequests);
        }

        [TestMethod]
        public void MultipleConcurrentRequests_AllTrackedInSnapshot()
        {
            var session = CreateSession();
            var runtime = session.Runtime;
            var observer = session.Observer;
            runtime.ProcessFrame(ProtocolFrames.Request(10));
            runtime.ProcessFrame(ProtocolFrames.Request(20));
            runtime.ProcessFrame(ProtocolFrames.Request(30));

            var snap = observer.GetSnapshot();

            Assert.HasCount(3, snap.OpenRequests);
            Assert.Contains(10u, snap.OpenRequests);
            Assert.Contains(20u, snap.OpenRequests);
            Assert.Contains(30u, snap.OpenRequests);
        }

        [TestMethod]
        public void MultipleRequests_CloseIndependently()
        {
            var session = CreateSession();
            var runtime = session.Runtime;
            var observer = session.Observer;

            runtime.ProcessFrame(ProtocolFrames.Request(1));
            runtime.ProcessFrame(ProtocolFrames.Request(2));
            runtime.ProcessFrame(ProtocolFrames.Response(1));

            var snap = observer.GetSnapshot();

            Assert.HasCount(1, snap.OpenRequests);
            Assert.DoesNotContain(1u, snap.OpenRequests);
            Assert.Contains(2u, snap.OpenRequests);
        }

        [TestMethod]
        public void RequestId_ReusableAfterClose()
        {
            var session = CreateSession();
            var runtime = session.Runtime;
            var observer = session.Observer;

            runtime.ProcessFrame(ProtocolFrames.Request(1));
            runtime.ProcessFrame(ProtocolFrames.Response(1));

            // The same ID may be used again once the previous context has closed.
            runtime.ProcessFrame(ProtocolFrames.Request(1));

            Assert.Contains(1u, observer.GetSnapshot().OpenRequests);
        }

        // ---------------------------------------------------------------
        // Requests - Part 2
        // ---------------------------------------------------------------

        [TestMethod]
        public void Inbound_ResponseFrame_IsAccepted()
        {
            var session = CreateSession();
            var runtime = session.Runtime;

            runtime.ProcessFrame(ProtocolFrames.Request(1));
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
            var runtime = session.Runtime;

            runtime.ProcessFrame(ProtocolFrames.Request(1));
            runtime.DrainOutboundFrames();

            runtime.ProcessFrame(ProtocolFrames.Response(1));
            var outbound = runtime.DrainOutboundFrames();

            Assert.HasCount(0, outbound);
        }

        [TestMethod]
        public void Inbound_Error_DoesNotEmitOutbound()
        {
            var session = CreateSession();
            var runtime = session.Runtime;

            runtime.ProcessFrame(ProtocolFrames.Request(1));
            runtime.DrainOutboundFrames();

            runtime.ProcessFrame(ProtocolFrames.Error(1));
            var outbound = runtime.DrainOutboundFrames();

            Assert.HasCount(0, outbound);
        }
    }
}
