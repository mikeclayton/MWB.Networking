using MWB.Networking.Layer0_Transport.Driver.UnitTests.Helpers;
using MWB.Networking.Layer1_Framing.Codec.Frames;

namespace MWB.Networking.Layer0_Transport.Driver.UnitTests;

// ============================================================
//  Lifecycle tests
//
//  Verify the Start → (run) → Dispose lifecycle contract:
//  - Start() must activate the read-and-decode loop.
//  - Dispose() must be safe to call before Start(), after
//    shutdown, and more than once.
//  - Dispose() must unsubscribe from transport events so
//    that subsequent transport signals are silently ignored.
// ============================================================

[TestClass]
public sealed class LifecycleTests
{
    public TestContext TestContext { get; set; } = null!;

    // ------------------------------------------------------------------
    // Start
    // ------------------------------------------------------------------

    /// <summary>
    /// After Start() returns, the I/O loop must be running: a queued EOF
    /// causes the driver's Closed event to fire without any further stimulus.
    /// </summary>
    [TestMethod]
    public async Task Start_BeginsReadLoop_ClosedFiresOnEof()
    {
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        var driverClosed = new TaskCompletionSource();
        driver.Closed += () => driverClosed.TrySetResult();

        driver.Start();

        transport.EnqueueEof();

        await driverClosed.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
    }

    /// <summary>
    /// After Start() returns, injecting bytes and then an EOF causes frames
    /// to be decoded and FrameReceived to fire — confirming the loop is active.
    /// </summary>
    [TestMethod]
    public async Task Start_BeginsReadLoop_FrameReceivedFiresOnData()
    {
        var pipeline = TestPipeline.CreateLengthPrefixed();
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, pipeline);

        var receivedFrames = new List<NetworkFrame>();
        var driverClosed = new TaskCompletionSource();
        driver.FrameReceived += f => receivedFrames.Add(f);
        driver.Closed += () => driverClosed.TrySetResult();

        driver.Start();

        var frame = NetworkFrames.Request(requestId: 1);
        transport.EnqueueBytes(TestPipeline.EncodeToBytes(pipeline, frame));
        transport.EnqueueEof();

        await driverClosed.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        Assert.HasCount(1, receivedFrames);
    }

    // ------------------------------------------------------------------
    // Dispose
    // ------------------------------------------------------------------

    /// <summary>
    /// Dispose() before Start() must not throw; the driver is cleanly
    /// torn down without ever having pumped any reads.
    /// </summary>
    [TestMethod]
    public void Dispose_BeforeStart_DoesNotThrow()
    {
        var transport = new FakeTransportStack();
        var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        // Must not throw
        driver.Dispose();
    }

    /// <summary>
    /// Calling Dispose() a second time must be a no-op and must not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var transport = new FakeTransportStack();
        var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        driver.Dispose();
        driver.Dispose(); // must not throw
    }

    /// <summary>
    /// Dispose() after the driver has already shut down via a clean close
    /// must not throw or raise additional events.
    /// </summary>
    [TestMethod]
    public async Task Dispose_AfterCleanClose_DoesNotThrow()
    {
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        var driverClosed = new TaskCompletionSource();
        driver.Closed += () => driverClosed.TrySetResult();

        driver.Start();
        transport.EnqueueEof();

        await driverClosed.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        // Should not throw even though the driver is already shut down
        driver.Dispose();
    }

    // ------------------------------------------------------------------
    // Dispose unsubscribes transport events
    // ------------------------------------------------------------------

    /// <summary>
    /// After Dispose(), raising TransportClosed on the transport must not
    /// cause the driver's Closed event to fire — the driver must have
    /// unsubscribed during disposal.
    /// </summary>
    [TestMethod]
    public void Dispose_UnsubscribesTransportClosed_SubsequentSignalIgnored()
    {
        var transport = new FakeTransportStack();
        var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        var closedFiredAfterDispose = false;
        driver.Closed += () => closedFiredAfterDispose = true;

        driver.Dispose();

        // Simulate the transport firing its event after the driver is disposed
        transport.RaiseTransportClosed();

        Assert.IsFalse(closedFiredAfterDispose,
            "Closed must not fire after Dispose() has unsubscribed from TransportClosed.");
    }

    /// <summary>
    /// After Dispose(), raising TransportFaulted on the transport must not
    /// cause the driver's Faulted event to fire.
    /// </summary>
    [TestMethod]
    public void Dispose_UnsubscribesTransportFaulted_SubsequentSignalIgnored()
    {
        var transport = new FakeTransportStack();
        var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        var faultedAfterDispose = false;
        driver.Faulted += _ => faultedAfterDispose = true;

        driver.Dispose();

        transport.RaiseTransportFaulted(new IOException("post-dispose fault"));

        Assert.IsFalse(faultedAfterDispose,
            "Faulted must not fire after Dispose() has unsubscribed from TransportFaulted.");
    }
}
