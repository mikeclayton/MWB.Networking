using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Session.Frames;
using MWB.Networking.Layer2_Protocol.Session.UnitTests.Helpers;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

namespace _ProtocolSession;

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

    [TestCleanup]
    public void Cleanup()
    {
        // force any unobserved exceptions from finalizers to surface during
        // test runs rather than being silently ignored - this makes it easier
        // to determine *which* test caused the issue (and fix it!).
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    // ---------------------------------------------------------------
    // Streams - Invariants
    // ---------------------------------------------------------------

    [TestMethod]
    public void StreamOpen_MissingStreamId_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        var frame = ProtocolFrameGenerator.CreateInvalidProtocolFrame(
            ProtocolFrameKind.StreamOpen);

        Assert.Throws<ProtocolException>(() => processor.ProcessFrame(frame));
    }

    [TestMethod]
    public void DuplicateStreamId_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));

        Assert.Throws<ProtocolException>(
            () => processor.ProcessFrame(ProtocolFrames.StreamOpen(2)));
    }

    [TestMethod]
    public void StreamData_UnknownStreamId_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        Assert.Throws<ProtocolException>(
            () => processor.ProcessFrame(ProtocolFrames.StreamData(99, new byte[] { 1 })));
    }

    [TestMethod]
    public void StreamClose_UnknownStreamId_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        Assert.Throws<ProtocolException>(
            () => processor.ProcessFrame(ProtocolFrames.StreamClose(99)));
    }

    [TestMethod]
    public void StreamData_MissingStreamId_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        var frame = ProtocolFrameGenerator.CreateInvalidProtocolFrame(
            ProtocolFrameKind.StreamData);

        Assert.Throws<ProtocolException>(() => processor.ProcessFrame(frame));
    }

    [TestMethod]
    public void StreamData_AfterClose_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.StreamClose(2));

        Assert.Throws<ProtocolException>(
            () => processor.ProcessFrame(ProtocolFrames.StreamData(2, new byte[] { 1 })));
    }

    [TestMethod]
    public void StreamClose_Twice_ThrowsProtocolException()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.StreamOpen(2));
        processor.ProcessFrame(ProtocolFrames.StreamClose(2));

        // Stream is removed after close, so the second close hits an unknown-id error.
        Assert.Throws<ProtocolException>(
            () => processor.ProcessFrame(ProtocolFrames.StreamClose(2)));
    }

            // ---------------------------------------------------------------
            // Parity enforcement (B1)
            // ---------------------------------------------------------------

            [TestMethod]
            public void StreamOpen_WithWrongParity_ThrowsProtocolException()
            {
                // The session uses Odd parity for outbound IDs, so valid inbound IDs from
                // the peer must be Even. Receiving a StreamOpen with an Odd ID (same parity
                // as local outbound) must be rejected — accepting it guarantees a future
                // collision when the local peer allocates the same ID outbound.
                var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
                var processor = session.Processor;

                // ID 1 is Odd — same parity as this session's outbound IDs. Must be rejected.
                Assert.Throws<ProtocolException>(
                    () => processor.ProcessFrame(ProtocolFrames.StreamOpen(1)));
            }

            [TestMethod]
            public void StreamOpen_WithCorrectParity_IsAccepted()
            {
                // ID 2 is Even — the peer's parity for an Odd local session.
                var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
                var processor = session.Processor;

                processor.ProcessFrame(ProtocolFrames.StreamOpen(2));

                Assert.Contains(2u, session.Diagnostics.GetSnapshot().OpenStreams);
            }

            // ---------------------------------------------------------------
            // StreamData on a request-scoped stream after the request closes (B3)
            // ---------------------------------------------------------------

            [TestMethod]
            public void StreamData_OnRequestScopedStream_AfterRequestClosed_ThrowsProtocolException()
            {
                // When an outgoing request that owns a request-scoped stream is responded
                // to by the peer, sending further StreamData on the stream must be rejected
                // as a ProtocolException (not an InvalidOperationException — that would be
                // an unhandled application exception leaking through the protocol boundary).
                var session = ProtocolSessionHelper.CreateOddProtocolSession(NullLogger.Instance);
                var processor = session.Processor;

                // Our side opens a request and a request-scoped stream.
                var outgoing = session.Commands.SendRequest();
                var stream = outgoing.OpenRequestStream(null);

                // Peer closes the request with a Response.
                processor.ProcessFrame(ProtocolFrames.Response(outgoing.RequestId));

                // Attempting StreamData on the now-dead stream must produce a ProtocolException.
                Assert.Throws<ProtocolException>(
                    () => stream.SendData(new byte[] { 0xFF }));
            }
        }
