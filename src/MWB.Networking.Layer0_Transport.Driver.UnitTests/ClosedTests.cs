using MWB.Networking.Layer0_Transport.Driver.UnitTests.Helpers;

namespace MWB.Networking.Layer0_Transport.Driver.UnitTests;

// ============================================================
//  Closed event tests
//
//  Verify that TransportDriver.Closed fires in all the
//  circumstances that represent a clean, graceful shutdown:
//
//  1. Read() returns 0  (EOF from the remote peer).
//  2. The transport raises TransportClosed directly.
//
//  Also verify idempotence: no matter how many triggers
//  occur, Closed fires at most once.
// ============================================================

[TestClass]
public sealed class ClosedTests
{
    public TestContext TestContext { get; set; } = null!;

    // ------------------------------------------------------------------
    // Read returns 0
    // ------------------------------------------------------------------

    /// <summary>
    /// When Read() returns 0 (clean EOF), the driver must raise Closed.
    /// </summary>
    [TestMethod]
    public async Task Closed_WhenReadReturnsZero_IsFired()
    {
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        var closedFired = false;
        var driverClosed = new TaskCompletionSource();
        driver.Closed += () =>
        {
            closedFired = true;
            driverClosed.TrySetResult();
        };

        driver.Start();
        transport.EnqueueEof();

        await driverClosed.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        Assert.IsTrue(closedFired, "Closed must fire when Read returns 0.");
    }

    /// <summary>
    /// When Read() returns 0, Closed must be raised exactly once.
    /// </summary>
    [TestMethod]
    public async Task Closed_WhenReadReturnsZero_FiredExactlyOnce()
    {
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        var closedCount = 0;
        var driverClosed = new TaskCompletionSource();
        driver.Closed += () =>
        {
            closedCount++;
            driverClosed.TrySetResult();
        };

        driver.Start();
        transport.EnqueueEof();

        await driverClosed.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        // Give any hypothetical second firing a moment to arrive
        await Task.Delay(50, TestContext.CancellationToken);

        Assert.AreEqual(1, closedCount, "Closed must fire exactly once.");
    }

    // ------------------------------------------------------------------
    // TransportClosed event
    // ------------------------------------------------------------------

    /// <summary>
    /// When the transport raises TransportClosed (before Start), the driver
    /// must propagate it as its own Closed event.
    /// </summary>
    [TestMethod]
    public void Closed_WhenTransportRaisesClosedBeforeStart_IsFired()
    {
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        var closedFired = false;
        driver.Closed += () => closedFired = true;

        transport.RaiseTransportClosed();

        Assert.IsTrue(closedFired,
            "Closed must fire when the transport raises TransportClosed.");
    }

    /// <summary>
    /// When the transport raises TransportClosed (after Start), the driver
    /// must propagate it as its own Closed event.
    /// </summary>
    [TestMethod]
    public async Task Closed_WhenTransportRaisesClosedAfterStart_IsFired()
    {
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        var closedFired = false;
        var driverClosed = new TaskCompletionSource();
        driver.Closed += () =>
        {
            closedFired = true;
            driverClosed.TrySetResult();
        };

        driver.Start();

        // Raise on the transport side, and also unblock the I/O loop's Read()
        transport.RaiseTransportClosed();
        transport.EnqueueEof();

        await driverClosed.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        Assert.IsTrue(closedFired,
            "Closed must fire when the transport raises TransportClosed after Start().");
    }

    // ------------------------------------------------------------------
    // Idempotence
    // ------------------------------------------------------------------

    /// <summary>
    /// Even if both Read-returns-zero and TransportClosed fire concurrently,
    /// Closed must be raised at most once.
    /// </summary>
    [TestMethod]
    public async Task Closed_WhenMultipleTriggersOccur_FiresAtMostOnce()
    {
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        var closedCount = 0;
        var driverClosed = new TaskCompletionSource();
        driver.Closed += () =>
        {
            Interlocked.Increment(ref closedCount);
            driverClosed.TrySetResult();
        };

        driver.Start();

        // Trigger two simultaneous sources of "closed"
        transport.RaiseTransportClosed();
        transport.EnqueueEof();

        await driverClosed.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        await Task.Delay(50, TestContext.CancellationToken);

        Assert.AreEqual(1, closedCount,
            "Closed must fire at most once even with multiple concurrent triggers.");
    }

    // ------------------------------------------------------------------
    // Post-close state
    // ------------------------------------------------------------------

    /// <summary>
    /// After Closed has fired, Send must throw InvalidOperationException
    /// because the connection is gone.
    /// </summary>
    [TestMethod]
    public async Task Closed_SubsequentSendThrowsInvalidOperationException()
    {
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        var driverClosed = new TaskCompletionSource();
        driver.Closed += () => driverClosed.TrySetResult();

        driver.Start();
        transport.EnqueueEof();

        await driverClosed.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        Assert.ThrowsExactly<InvalidOperationException>(
            () => driver.Send(MWB.Networking.Layer1_Framing.Codec.Frames.NetworkFrames.Request(requestId: 1)),
            "Send must throw after the driver has entered the closed state.");
    }
}
