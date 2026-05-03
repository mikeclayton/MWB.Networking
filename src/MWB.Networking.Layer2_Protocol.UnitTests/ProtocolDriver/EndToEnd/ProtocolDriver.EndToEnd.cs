using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

namespace _ProtocolDriver;

[TestClass]
public sealed partial class EndToEnd
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

    //
    // conflicts with:
    //
    // * StreamClose_Twice_ThrowsProtocolException
    // * StreamClose_UnknownStreamId_ThrowsProtocolException
    //
    //[TestMethod]
    //public void StreamClose_Is_Idempotent()
    //{
    //    var logger = NullLogger.Instance;
    //    var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
    //    var processor = session.Processor;
    //
    //    // Arrange: open a request and a request-scoped stream
    //    processor.ProcessFrame(ProtocolFrames.Request(1));
    //    processor.ProcessFrame(ProtocolFrames.StreamOpen(
    //        requestId: 1,
    //        streamId: 1));
    //
    //    // Sanity check: stream is open
    //    var snapshotBefore = session.Diagnostics.GetSnapshot();
    //    Assert.Contains(1u, snapshotBefore.OpenStreams);
    //
    //    // Act: close the stream twice via protocol frames
    //    processor.ProcessFrame(ProtocolFrames.StreamClose(1));
    //    processor.ProcessFrame(ProtocolFrames.StreamClose(1)); // second close
    //
    //    // Assert: no streams remain, no exception, no further mutation
    //    var snapshotAfter = session.Diagnostics.GetSnapshot();
    //    Assert.IsEmpty(snapshotAfter.OpenStreams);
    //}

    [TestMethod]
    public void StreamTeardown_DoesNotAffect_OtherRequests()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        // Arrange: create two requests
        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.ProcessFrame(ProtocolFrames.Request(3));

        // Open a stream scoped to request 1
        processor.ProcessFrame(ProtocolFrames.StreamOpen(
            requestId: 1,
            streamId: 1));

        // Act: close the stream
        processor.ProcessFrame(ProtocolFrames.StreamClose(1));

        // Assert: request 3 is still open
        var snap = session.Diagnostics.GetSnapshot();
        Assert.Contains(3u, snap.OpenRequests);
    }

    [TestMethod]
    public void ObserverCallbacks_DoNotTriggerAdditional_Teardown()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        // Arrange: attach observer that records invocation
        var observerInvoked = false;

        session.Observer.StreamClosed += (_, _) =>
        {
            observerInvoked = true;
            // IMPORTANT: observer does nothing protocol-mutating
        };

        processor.ProcessFrame(ProtocolFrames.Request(1));
        processor.ProcessFrame(ProtocolFrames.StreamOpen(
            requestId: 1,
            streamId: 1));

        // Act: close the stream
        processor.ProcessFrame(ProtocolFrames.StreamClose(1));

        // Assert: observer was invoked
        Assert.IsTrue(observerInvoked);

        // Assert: protocol state is stable
        var snap = session.Diagnostics.GetSnapshot();
        Assert.IsEmpty(snap.OpenStreams);
        Assert.Contains(1u, snap.OpenRequests);
    }
}
