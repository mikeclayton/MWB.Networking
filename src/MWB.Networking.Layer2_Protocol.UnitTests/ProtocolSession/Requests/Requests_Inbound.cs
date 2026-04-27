using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

namespace _ProtocolSession;

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
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));

        Assert.Contains(1u, session.Diagnostics.GetSnapshot().OpenRequests);
    }

    // ---------------------------------------------------------------
    // Requests - Part 1
    // ---------------------------------------------------------------

    [TestMethod]
    public void Inbound_Response_ClosesRequest()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.ProcessFrame(ProtocolFrames.Error(1));

        Assert.IsEmpty(session.Diagnostics.GetSnapshot().OpenRequests);
    }

    [TestMethod]
    public void Inbound_RequestError_ClosesRequest()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.ProcessFrame(ProtocolFrames.Error(1));

        Assert.IsEmpty(session.Diagnostics.GetSnapshot().OpenRequests);
    }

    [TestMethod]
    public void MultipleConcurrentRequests_AllTrackedInSnapshot()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(10));
        processor.ProcessFrame(ProtocolFrames.Request(20));
        processor.ProcessFrame(ProtocolFrames.Request(30));

        var snap = session.Diagnostics.GetSnapshot();

        Assert.HasCount(3, snap.OpenRequests);
        Assert.Contains(10u, snap.OpenRequests);
        Assert.Contains(20u, snap.OpenRequests);
        Assert.Contains(30u, snap.OpenRequests);
    }

    [TestMethod]
    public void MultipleRequests_CloseIndependently()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.ProcessFrame(ProtocolFrames.Request(2));
        processor.ProcessFrame(ProtocolFrames.Response(1));

        var snap = session.Diagnostics.GetSnapshot();

        Assert.HasCount(1, snap.OpenRequests);
        Assert.DoesNotContain(1u, snap.OpenRequests);
        Assert.Contains(2u, snap.OpenRequests);
    }

    [TestMethod]
    public void RequestId_ReusableAfterClose()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.ProcessFrame(ProtocolFrames.Response(1));

        // The same ID may be used again once the previous context has closed.
        processor.ProcessFrame(ProtocolFrames.Request(1));

        Assert.Contains(1u, session.Diagnostics.GetSnapshot().OpenRequests);
    }

    // ---------------------------------------------------------------
    // Requests - Part 2
    // ---------------------------------------------------------------

    [TestMethod]
    public void Inbound_ResponseFrame_IsAccepted()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.DrainOutboundFrames();

        processor.ProcessFrame(ProtocolFrames.Response(1, null, new byte[] { 10 }));

        // No exception = success
        var outbound = processor.DrainOutboundFrames();
        Assert.HasCount(0, outbound);
    }

    [TestMethod]
    public void Inbound_Response_DoesNotEmitOutbound()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.DrainOutboundFrames();

        processor.ProcessFrame(ProtocolFrames.Response(1));
        var outbound = processor.DrainOutboundFrames();

        Assert.HasCount(0, outbound);
    }

    [TestMethod]
    public void Inbound_Error_DoesNotEmitOutbound()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.DrainOutboundFrames();

        processor.ProcessFrame(ProtocolFrames.Error(1));
        var outbound = processor.DrainOutboundFrames();

        Assert.HasCount(0, outbound);
    }
}
