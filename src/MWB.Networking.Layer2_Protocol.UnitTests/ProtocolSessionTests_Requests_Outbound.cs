using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Requests.Api;
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
    public sealed partial class Requests_Outbound
    {
        public TestContext TestContext
        {
            get;
            set;
        }

        // ---------------------------------------------------------------
        // Outbound frame emission
        // ---------------------------------------------------------------

        [TestMethod]
        public void Request_IsRaisedToApplication_AndNotEmittedOutbound()
        {
            var session = ProtocolSessionHelper.CreateNullSession();
            var runtime = session.Runtime;

            IncomingRequest? received = null;
            int callCount = 0;

            session.Observer.RequestReceived += (req, payload) =>
            {
                received = req;
                callCount++;
            };

            runtime.ProcessFrame(ProtocolFrames.Request(1));

            Assert.AreEqual(1, callCount);
            Assert.IsNotNull(received);
            Assert.IsEmpty(runtime.DrainOutboundFrames());
        }


        [TestMethod]
        public void Respond_EmitsResponseToOutbound()
        {
            var session = ProtocolSessionHelper.CreateNullSession();
            var runtime = session.Runtime;

            IncomingRequest? request = null;

            session.Observer.RequestReceived += (req, payload) =>
            {
                request = req;
            };

            runtime.ProcessFrame(ProtocolFrames.Request(1));
            runtime.DrainOutboundFrames();

            var payload = new byte[] { 0xAB, 0xCD };

            Assert.IsNotNull(request);
            request.Respond(payload);

            var outbound = runtime.DrainOutboundFrames();

            Assert.HasCount(1, outbound);
            Assert.AreEqual(ProtocolFrameKind.Response, outbound[0].Kind);
            Assert.AreEqual(1u, outbound[0].RequestId);
            CollectionAssert.AreEqual(payload, outbound[0].Payload.ToArray());
        }

        [TestMethod]

        public void Request_AllowsOnlyOneResponse_SecondRespondThrows()
        {
            var session = ProtocolSessionHelper.CreateNullSession();
            var runtime = session.Runtime;

            IncomingRequest? request = null;

            session.Observer.RequestReceived += (req, payload) =>
            {
                request = req;
            };

            // Peer sends request
            runtime.ProcessFrame(ProtocolFrames.Request(1));

            // First response is allowed
            request!.Respond(new byte[] { 0xA1 });

            // Second response must be rejected
            Assert.Throws<InvalidOperationException>(() =>
            {
                request.Respond(new byte[] { 0xA2 });
            });

            var outbound = runtime.DrainOutboundFrames();

            Assert.HasCount(1, outbound);
            Assert.AreEqual(ProtocolFrameKind.Response, outbound[0].Kind);
            CollectionAssert.AreEqual(new byte[] { 0xA1 }, outbound[0].Payload.ToArray());
        }

        [TestMethod]
        public void TwoConcurrentRequests_ResponsesEmittedInOrder()
        {
            var session = ProtocolSessionHelper.CreateNullSession();
            var runtime = session.Runtime;

            IncomingRequest? req1 = null;
            IncomingRequest? req2 = null;

            session.Observer.RequestReceived += (req, payload) =>
            {
                if (req.Context.RequestId == 1)
                {
                    req1 = req;
                }
                else if (req.Context.RequestId == 2)
                {
                    req2 = req;
                }
            };

            // Open two requests
            runtime.ProcessFrame(ProtocolFrames.Request(1));
            runtime.ProcessFrame(ProtocolFrames.Request(2));
            runtime.DrainOutboundFrames();

            // Respond to both, in order
            Assert.IsNotNull(req1);
            Assert.IsNotNull(req2);
            req1.Respond(new byte[] { 0x11 });
            req2.Respond(new byte[] { 0x22 });

            var outbound = runtime.DrainOutboundFrames();

            Assert.HasCount(2, outbound);
            Assert.AreEqual(1u, outbound[0].RequestId);
            Assert.AreEqual(2u, outbound[1].RequestId);
        }

        [TestMethod]
        public void Request_IsClosedAfterSingleResponse()
        {
            var session = ProtocolSessionHelper.CreateNullSession();
            var runtime = session.Runtime;

            IncomingRequest? request = null;

            session.Observer.RequestReceived += (req, payload) =>
            {
                request = req;
            };

            runtime.ProcessFrame(ProtocolFrames.Request(1));

            Assert.IsNotNull(request);
            request.Respond(new byte[] { 0xAA });

            Assert.DoesNotContain(1u, session.Diagnostics.GetSnapshot().OpenRequests);
        }

        [TestMethod]
        public void Request_SecondResponse_IsRejected()
        {
            var session = ProtocolSessionHelper.CreateNullSession();
            var runtime = session.Runtime;

            IncomingRequest? request = null;

            session.Observer.RequestReceived += (req, payload) =>
            {
                request = req;
            };

            runtime.ProcessFrame(ProtocolFrames.Request(1));

            Assert.IsNotNull(request);
            request.Respond(new byte[] { 1 });

            Assert.Throws<InvalidOperationException>(() =>
            {
                request.Respond(new byte[] { 2 });
            });

            Assert.DoesNotContain(1u, session.Diagnostics.GetSnapshot().OpenRequests);
        }
    }
}
