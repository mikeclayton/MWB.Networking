using MWB.Networking.Layer2_Protocol;
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
    public sealed class Requests
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

        // Internal ProtocolFrame ctor is accessible via InternalsVisibleTo.
        private static ProtocolFrame MakeError(uint requestId) =>
            new(ProtocolFrameKind.Error, null, requestId, null, ReadOnlyMemory<byte>.Empty);

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

        [TestMethod]
        public void Request_IsClosedAfterSingleResponse()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            IncomingRequest? request = null;

            session.RequestReceived += (req, payload) =>
            {
                request = req;
            };

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));

            Assert.IsNotNull(request);
            request.Respond(new byte[] { 0xAA });

            Assert.DoesNotContain(1u, session.Snapshot().OpenRequests);
        }

        [TestMethod]
        public void Request_SecondResponse_IsRejected()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            IncomingRequest? request = null;

            session.RequestReceived += (req, payload) =>
            {
                request = req;
            };

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));

            Assert.IsNotNull(request);
            request.Respond(new byte[] { 1 });

            Assert.Throws<InvalidOperationException>(() =>
            {
                request.Respond(new byte[] { 2 });
            });

            Assert.DoesNotContain(1u, session.Snapshot().OpenRequests);
        }

        [TestMethod]
        public void CannotEmitStreamOpen_AfterResponse()
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

            // Send the single response (closes the request)
            Assert.IsNotNull(request);
            request.Respond(new byte[] { 0xAA });

            // Attempting to emit a stream after the response is invalid
            Assert.Throws<InvalidOperationException>(() =>
            {
                session.EnqueueOutboundFrame(ProtocolFrames.StreamOpen(10, Empty));
            });
        }

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

        [TestMethod]
        public void CompleteRequest_ClosesRequest()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.CompleteRequest(1));

            Assert.IsEmpty(session.Snapshot().OpenRequests);
        }

        [TestMethod]
        public void Error_ClosesRequest()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.ProcessFrame(MakeError(1));

            Assert.IsEmpty(session.Snapshot().OpenRequests);
        }

        [TestMethod]
        public void CancelRequest_ClosesRequest()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.CancelRequest(1));

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
            runtime.ProcessFrame(ProtocolFrames.CompleteRequest(1));

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
            runtime.ProcessFrame(ProtocolFrames.CompleteRequest(1));

            // The same ID may be used again once the previous context has closed.
            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));

            Assert.Contains(1u, session.Snapshot().OpenRequests);
        }

        // ---------------------------------------------------------------
        // Outbound frame emission
        // ---------------------------------------------------------------


        [TestMethod]
        public void Request_IsRaisedToApplication_AndNotEmittedOutbound()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            IncomingRequest? received = null;
            int callCount = 0;

            session.RequestReceived += (req, payload) =>
            {
                received = req;
                callCount++;
            };

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));

            Assert.AreEqual(1, callCount);
            Assert.IsNotNull(received);
            Assert.IsEmpty(runtime.DrainOutboundFrames());
        }


        [TestMethod]
        public void Respond_EmitsResponseToOutbound()
        {
            var session = (ProtocolSession)CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            IncomingRequest? request = null;

            session.RequestReceived += (req, payload) =>
            {
                request = req;
            };

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
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
        public void Inbound_ResponseFrame_IsRejected()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));

            Assert.Throws<ProtocolException>(() =>
                runtime.ProcessFrame(ProtocolFrames.Response(1, new byte[] { 10 }))
            );
        }

        [TestMethod]
        public void CompleteRequest_IsEmittedToOutbound()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.DrainOutboundFrames();

            runtime.ProcessFrame(ProtocolFrames.CompleteRequest(1));
            var outbound = runtime.DrainOutboundFrames();

            Assert.HasCount(1, outbound);
            Assert.AreEqual(ProtocolFrameKind.Complete, outbound[0].Kind);
            Assert.AreEqual(1u, outbound[0].RequestId);
        }

        [TestMethod]
        public void Error_IsEmittedToOutbound()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.DrainOutboundFrames();

            runtime.ProcessFrame(MakeError(1));
            var outbound = runtime.DrainOutboundFrames();

            Assert.HasCount(1, outbound);
            Assert.AreEqual(ProtocolFrameKind.Error, outbound[0].Kind);
            Assert.AreEqual(1u, outbound[0].RequestId);
        }

        [TestMethod]
        public void CancelRequest_IsEmittedToOutbound()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(5, Empty));
            runtime.DrainOutboundFrames();

            runtime.ProcessFrame(ProtocolFrames.CancelRequest(5));
            var outbound = runtime.DrainOutboundFrames();

            Assert.HasCount(1, outbound);
            Assert.AreEqual(ProtocolFrameKind.Cancel, outbound[0].Kind);
            Assert.AreEqual(5u, outbound[0].RequestId);
        }

        [TestMethod]

        public void Request_AllowsOnlyOneResponse_SecondRespondThrows()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            IncomingRequest? request = null;

            session.RequestReceived += (req, payload) =>
            {
                request = req;
            };

            // Peer sends request
            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));

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
            var session = (ProtocolSession)CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            IncomingRequest? req1 = null;
            IncomingRequest? req2 = null;

            session.RequestReceived += (req, payload) =>
            {
                if (req.RequestId == 1)
                {
                    req1 = req;
                }
                else if (req.RequestId == 2)
                {
                    req2 = req;
                }
            };

            // Open two requests
            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.Request(2, Empty));
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
                () => runtime.ProcessFrame(ProtocolFrames.Response(99, Empty)));
        }

        [TestMethod]
        public void Complete_UnknownRequestId_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            Assert.Throws<ProtocolException>(
                () => runtime.ProcessFrame(ProtocolFrames.CompleteRequest(99)));
        }

        [TestMethod]
        public void Cancel_UnknownRequestId_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            Assert.Throws<ProtocolException>(
                () => runtime.ProcessFrame(ProtocolFrames.CancelRequest(99)));
        }

        [TestMethod]
        public void Error_UnknownRequestId_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            Assert.Throws<ProtocolException>(() => runtime.ProcessFrame(MakeError(99)));
        }

        [TestMethod]
        public void Response_AfterCompleteRequest_ThrowsProtocolException()
        {
            // Complete removes the context; the same RequestId is now unknown.
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.CompleteRequest(1));

            Assert.Throws<ProtocolException>(
                () => runtime.ProcessFrame(ProtocolFrames.Response(1, Empty)));
        }

        [TestMethod]
        public void CompleteRequest_Twice_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            runtime.ProcessFrame(ProtocolFrames.Request(1, Empty));
            runtime.ProcessFrame(ProtocolFrames.CompleteRequest(1));

            Assert.Throws<ProtocolException>(
                () => runtime.ProcessFrame(ProtocolFrames.CompleteRequest(1)));
        }

        [TestMethod]
        public void Response_MissingRequestId_ThrowsProtocolException()
        {
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            var frame = new ProtocolFrame(ProtocolFrameKind.Response, null, null, null, Empty);

            Assert.Throws<ProtocolException>(() => runtime.ProcessFrame(frame));
        }

        [TestMethod]
        public void Cancel_WithOnlyStreamId_ThrowsProtocolException()
        {
            // Cancel is always routed through request handling; a null RequestId always throws.
            var session = CreateSession();
            var runtime = (IProtocolSessionRuntime)session;

            Assert.Throws<ProtocolException>(
                () => runtime.ProcessFrame(ProtocolFrames.CancelRequest(5)));
        }
    }
}
