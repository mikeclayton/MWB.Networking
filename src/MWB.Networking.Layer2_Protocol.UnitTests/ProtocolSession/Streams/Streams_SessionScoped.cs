using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

namespace _ProtocolSession;

/// <summary>
/// Tests for the request lifecycle: Request → Response* → Complete | Error | Cancel.
/// Covers snapshot state, outbound frame emission, and protocol violation guards.
/// </summary>
[TestClass]
public sealed partial class Streams_SessionScoped
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
    // Streams - Session scoped
    // ---------------------------------------------------------------

    [TestMethod]
    public void StreamsMayBeOpenedIndependentlyOfRequests()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        IncomingRequest? request = null;

        session.Observer.RequestReceived += (req, payload) =>
        {
            request = req;
        };

        // Open a request
        processor.ProcessFrame(ProtocolFrames.Request(1));

        // Respond, closing the request
        Assert.IsNotNull(request);
        request.Respond(new byte[] { 0xAA });

        // Open an independent stream (inbound)
        processor.ProcessFrame(ProtocolFrames.StreamOpen(10));

        var snapshot = session.Diagnostics.GetSnapshot();

        // Request is closed
        Assert.DoesNotContain(1u, snapshot.OpenRequests);

        // Independent stream exists
        Assert.Contains(10u, snapshot.OpenStreams);
    }

    [TestMethod]
    public void StreamOpen_IsEmittedToOutbound()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        var metadata = new byte[] { 0x01, 0x02 };

        // Open a session-scoped stream via intent-level API
        var stream = session.Commands.OpenSessionStream(metadata);

        var outbound = processor.DrainOutboundFrames();

        Assert.HasCount(1, outbound);
        Assert.AreEqual(ProtocolFrameKind.StreamOpen, outbound[0].Kind);
        Assert.AreEqual(stream.StreamId, outbound[0].StreamId);
        CollectionAssert.AreEqual(metadata, outbound[0].Payload.ToArray());
    }

    [TestMethod]
    public void StreamData_IsEmittedToOutbound()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        var data = new byte[] { 0xDE, 0xAD };

        // Open a session-scoped stream
        var stream = session.Commands.OpenSessionStream();

        // Discard StreamOpen
        processor.DrainOutboundFrames();

        // Send data via intent-level API
        stream.SendData(data);

        var outbound = processor.DrainOutboundFrames();

        Assert.HasCount(1, outbound);
        Assert.AreEqual(ProtocolFrameKind.StreamData, outbound[0].Kind);
        Assert.AreEqual(stream.StreamId, outbound[0].StreamId);
        CollectionAssert.AreEqual(data, outbound[0].Payload.ToArray());
    }

    [TestMethod]
    public void StreamClose_IsEmittedToOutbound()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        // Open a session-scoped stream
        var stream = session.Commands.OpenSessionStream();

        // Discard the StreamOpen frame
        processor.DrainOutboundFrames();

        // Close the stream via intent-level API
        stream.Close();

        var outbound = processor.DrainOutboundFrames();

        Assert.HasCount(1, outbound);
        Assert.AreEqual(ProtocolFrameKind.StreamClose, outbound[0].Kind);
        Assert.AreEqual(stream.StreamId, outbound[0].StreamId);
    }
}
