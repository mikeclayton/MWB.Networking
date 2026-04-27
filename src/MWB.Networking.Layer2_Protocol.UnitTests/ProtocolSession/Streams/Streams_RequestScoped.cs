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
public sealed partial class Streams_RequestScoped
{
    public TestContext TestContext
    {
        get;
        set;
    }

    // ---------------------------------------------------------------
    // Streams - Request scoped
    // ---------------------------------------------------------------

    [TestMethod]
    public void FullRequestScopedStreamLifecycle_AllFramesEmittedInOrder()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        IncomingRequest? request = null;

        session.Observer.RequestReceived += (req, _) => request = req;

        // Inbound request
        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.DrainOutboundFrames();

        Assert.IsNotNull(request);

        // Open request-scoped stream
        var stream = request.OpenRequestStream(1u);

        stream.SendData(new byte[] { 0xA1 });
        stream.SendData(new byte[] { 0xA2 });
        stream.Close();

        var outbound = processor.DrainOutboundFrames();

        Assert.HasCount(4, outbound);
        Assert.AreEqual(ProtocolFrameKind.StreamOpen, outbound[0].Kind);
        Assert.AreEqual(ProtocolFrameKind.StreamData, outbound[1].Kind);
        Assert.AreEqual(ProtocolFrameKind.StreamData, outbound[2].Kind);
        Assert.AreEqual(ProtocolFrameKind.StreamClose, outbound[3].Kind);
    }

    [TestMethod]
    public void CannotOpenRequestScopedStream_AfterResponse()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        IncomingRequest? request = null;

        session.Observer.RequestReceived += (req, payload) =>
        {
            request = req;
        };

        // Inbound request
        processor.ProcessFrame(ProtocolFrames.Request(1));

        // Respond to the request (closes it)
        Assert.IsNotNull(request);
        request.Respond(new byte[] { 0xAA });

        // Attempting to open a request-scoped stream after response is invalid
        Assert.Throws<InvalidOperationException>(() =>
        {
            request.OpenRequestStream(1u);
        });
    }
}
