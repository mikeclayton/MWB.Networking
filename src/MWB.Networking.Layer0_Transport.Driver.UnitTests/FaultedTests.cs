using MWB.Networking.Layer0_Transport.Driver.UnitTests.Helpers;

namespace MWB.Networking.Layer0_Transport.Driver.UnitTests;

// ============================================================
//  Faulted event tests
//
//  Verify that TransportDriver.Faulted fires in all the
//  circumstances that represent an abnormal, error condition:
//
//  1. Read() throws an exception (I/O error).
//  2. The transport raises TransportFaulted directly.
//  3. An encoded frame cannot be decoded (InvalidFrameEncoding).
//
//  Also verify:
//  - The exact exception is forwarded (not wrapped).
//  - Idempotence: Faulted fires at most once.
//  - Send throws after a fault.
// ============================================================

[TestClass]
public sealed class FaultedTests
{
    public TestContext TestContext { get; set; } = null!;

    // ------------------------------------------------------------------
    // Read throws
    // ------------------------------------------------------------------

    /// <summary>
    /// When Read() throws, the driver must raise Faulted with that exception.
    /// </summary>
    [TestMethod]
    public async Task Faulted_WhenReadThrows_IsFired()
    {
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        var driverFaulted = new TaskCompletionSource<Exception>();
        driver.Faulted += ex => driverFaulted.TrySetResult(ex);

        driver.Start();

        var injected = new IOException("simulated read error");
        transport.EnqueueException(injected);

        var received = await driverFaulted.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        Assert.IsNotNull(received, "Faulted must fire when Read throws.");
        Assert.AreSame(injected, received,
            "The exact exception from Read should be forwarded via Faulted.");
    }

    /// <summary>
    /// When Read() throws, Faulted must fire exactly once.
    /// </summary>
    [TestMethod]
    public async Task Faulted_WhenReadThrows_FiredExactlyOnce()
    {
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        var faultedCount = 0;
        var driverFaulted = new TaskCompletionSource<Exception>();
        driver.Faulted += ex =>
        {
            Interlocked.Increment(ref faultedCount);
            driverFaulted.TrySetResult(ex);
        };

        driver.Start();
        transport.EnqueueException(new IOException("read fault"));

        await driverFaulted.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        await Task.Delay(50, TestContext.CancellationToken);

        Assert.AreEqual(1, faultedCount, "Faulted must fire exactly once.");
    }

    // ------------------------------------------------------------------
    // TransportFaulted event
    // ------------------------------------------------------------------

    /// <summary>
    /// When the transport raises TransportFaulted (before Start), the driver
    /// must propagate it as its own Faulted event with the same exception.
    /// </summary>
    [TestMethod]
    public void Faulted_WhenTransportRaisesFaultedBeforeStart_IsFired()
    {
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        Exception? received = null;
        driver.Faulted += ex => received = ex;

        var injected = new InvalidOperationException("transport-side fault");
        transport.RaiseTransportFaulted(injected);

        Assert.IsNotNull(received,
            "Faulted must fire when the transport raises TransportFaulted.");
        Assert.AreSame(injected, received,
            "The exact exception raised by the transport must be forwarded.");
    }

    /// <summary>
    /// When the transport raises TransportFaulted (after Start), the driver
    /// must propagate it as its own Faulted event.
    /// </summary>
    [TestMethod]
    public async Task Faulted_WhenTransportRaisesFaultedAfterStart_IsFired()
    {
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        var driverFaulted = new TaskCompletionSource<Exception>();
        driver.Faulted += ex => driverFaulted.TrySetResult(ex);

        driver.Start();

        var injected = new IOException("transport-side fault after start");
        transport.RaiseTransportFaulted(injected);

        // Also unblock the I/O loop's blocking Read() call
        transport.EnqueueEof();

        var received = await driverFaulted.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        Assert.AreSame(injected, received,
            "The exact exception raised by the transport must be forwarded.");
    }

    // ------------------------------------------------------------------
    // InvalidFrameEncoding
    // ------------------------------------------------------------------

    /// <summary>
    /// When the accumulated decode buffer contains bytes that the codec cannot
    /// recognise as a valid frame (InvalidFrameEncoding), the driver must
    /// raise Faulted with an appropriate exception rather than silently
    /// discarding the data or entering an infinite loop.
    ///
    /// The NullTransportCodec is used here so that the corrupt bytes are
    /// passed directly to the network-frame codec for decoding, skipping
    /// length-prefix framing that would otherwise gate on the byte count.
    /// </summary>
    [TestMethod]
    public async Task Faulted_WhenFrameEncodingIsInvalid_IsFired()
    {
        // NullTransportCodec: all available bytes are treated as one complete
        // transport frame; the network codec then sees the raw garbage bytes.
        var pipeline = TestPipeline.CreateNullTransport();
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, pipeline);

        var driverFaulted = new TaskCompletionSource<Exception>();
        driver.Faulted += ex => driverFaulted.TrySetResult(ex);

        driver.Start();

        // Bytes that no valid NetworkFrame starts with (0xFF is not a defined FrameKind)
        transport.EnqueueBytes([0xFF, 0xFF, 0xFF, 0xFF]);
        // Unblock the I/O loop after the fault path has been taken
        transport.EnqueueEof();

        var received = await driverFaulted.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        Assert.IsNotNull(received,
            "Faulted must fire when the frame codec returns InvalidFrameEncoding.");
        Assert.IsInstanceOfType<InvalidOperationException>(received,
            "An InvalidFrameEncoding result must manifest as an InvalidOperationException fault.");
    }

    // ------------------------------------------------------------------
    // Idempotence
    // ------------------------------------------------------------------

    /// <summary>
    /// If both a Read fault and a TransportFaulted event arrive concurrently,
    /// Faulted must fire at most once.
    /// </summary>
    [TestMethod]
    public async Task Faulted_WhenMultipleTriggersOccur_FiresAtMostOnce()
    {
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        var faultedCount = 0;
        var driverFaulted = new TaskCompletionSource<Exception>();
        driver.Faulted += ex =>
        {
            Interlocked.Increment(ref faultedCount);
            driverFaulted.TrySetResult(ex);
        };

        driver.Start();

        var fault = new IOException("concurrent fault");
        transport.RaiseTransportFaulted(fault);
        transport.EnqueueException(new IOException("concurrent read fault"));

        await driverFaulted.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        await Task.Delay(50, TestContext.CancellationToken);

        Assert.AreEqual(1, faultedCount,
            "Faulted must fire at most once even with multiple concurrent triggers.");
    }

    // ------------------------------------------------------------------
    // Post-fault state
    // ------------------------------------------------------------------

    /// <summary>
    /// After Faulted has fired, Send must throw InvalidOperationException
    /// because the connection is broken.
    /// </summary>
    [TestMethod]
    public async Task Faulted_SubsequentSendThrowsInvalidOperationException()
    {
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        var driverFaulted = new TaskCompletionSource<Exception>();
        driver.Faulted += ex => driverFaulted.TrySetResult(ex);

        driver.Start();
        transport.EnqueueException(new IOException("fault"));

        await driverFaulted.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        Assert.ThrowsExactly<InvalidOperationException>(
            () => driver.Send(MWB.Networking.Layer1_Framing.Codec.Frames.NetworkFrames.Request(requestId: 1)),
            "Send must throw after the driver has entered the faulted state.");
    }

    // ------------------------------------------------------------------
    // Closed vs Faulted mutual exclusion
    // ------------------------------------------------------------------

    /// <summary>
    /// When the connection faults, Closed must not also fire.
    /// The two events are mutually exclusive.
    /// </summary>
    [TestMethod]
    public async Task Faulted_WhenReadThrows_ClosedDoesNotAlsoFire()
    {
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        var closedFired = false;
        var driverFaulted = new TaskCompletionSource<Exception>();
        driver.Closed += () => closedFired = true;
        driver.Faulted += ex => driverFaulted.TrySetResult(ex);

        driver.Start();
        transport.EnqueueException(new IOException("read fault"));

        await driverFaulted.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        await Task.Delay(50, TestContext.CancellationToken);

        Assert.IsFalse(closedFired,
            "Closed must not fire when the driver enters the faulted state.");
    }

    /// <summary>
    /// When the connection closes cleanly, Faulted must not also fire.
    /// The two events are mutually exclusive.
    /// </summary>
    [TestMethod]
    public async Task Closed_WhenReadReturnsZero_FaultedDoesNotAlsoFire()
    {
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        var faultedFired = false;
        var driverClosed = new TaskCompletionSource();
        driver.Faulted += _ => faultedFired = true;
        driver.Closed += () => driverClosed.TrySetResult();

        driver.Start();
        transport.EnqueueEof();

        await driverClosed.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        await Task.Delay(50, TestContext.CancellationToken);

        Assert.IsFalse(faultedFired,
            "Faulted must not fire when the driver closes cleanly.");
    }
}
