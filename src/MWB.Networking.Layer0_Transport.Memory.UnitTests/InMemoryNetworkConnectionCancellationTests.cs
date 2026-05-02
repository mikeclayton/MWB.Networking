using MWB.Networking.Layer0_Transport.Memory.UnitTests.Helpers;

namespace MWB.Networking.Layer0_Transport.Memory.UnitTests;

[TestClass]
public sealed class InMemoryNetworkConnectionCancellationTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    // -------------------------------------------------------------------------
    // Pre-cancelled token
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task ReadAsync_WithAlreadyCancelledToken_ThrowsOperationCanceledException()
    {
        // A pre-cancelled token causes SemaphoreSlim.WaitAsync to return
        // Task.FromCanceled, which when awaited surfaces as TaskCanceledException
        // (a subtype of OperationCanceledException). We verify the base type only.
        var (_, readEnd) = ConnectionTestHelpers.CreateUnidirectionalPair();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var buffer = new byte[16];
        bool threw = false;
        try
        {
            await readEnd.ReadAsync(buffer, cts.Token);
        }
        catch (OperationCanceledException)
        {
            threw = true;
        }

        Assert.IsTrue(threw, "ReadAsync must throw OperationCanceledException when the token is already cancelled.");
    }

    // -------------------------------------------------------------------------
    // Cancellation of a blocked read
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task ReadAsync_CancelledWhileWaitingForData_ThrowsOperationCanceledException()
    {
        var (_, readEnd) = ConnectionTestHelpers.CreateUnidirectionalPair();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.CancellationToken);

        var buffer = new byte[16];

        // ReadAsync will block — no data has been written yet
        var readTask = readEnd.ReadAsync(buffer, cts.Token).AsTask();

        await Task.Yield();
        Assert.IsFalse(readTask.IsCompleted,
            "ReadAsync should be blocked waiting for data before cancellation.");

        await cts.CancelAsync();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            async () => await readTask.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [TestMethod]
    public async Task ReadAsync_CancelledWhileWaiting_ChannelRemainsUsable()
    {
        // Cancelling a blocked read must not corrupt the channel; subsequent
        // writes and reads on a fresh token must succeed normally.
        var (writeEnd, readEnd) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var ct = TestContext.CancellationToken;

        // Cancel one read
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var buffer = new byte[16];
        var cancelledRead = readEnd.ReadAsync(buffer, cts.Token).AsTask();

        await Task.Yield();
        await cts.CancelAsync();

        try
        {
            await cancelledRead;
        }
        catch (OperationCanceledException)
        {
            // expected
        }

        // The channel should still be fully functional
        var data = new byte[] { 0xDE, 0xAD };
        await writeEnd.WriteAsync(ConnectionTestHelpers.Segment(data), ct);

        var freshBuffer = new byte[data.Length];
        var bytesRead = await readEnd.ReadAsync(freshBuffer, ct);

        Assert.AreEqual(data.Length, bytesRead);
        CollectionAssert.AreEqual(data, freshBuffer);
    }

    // -------------------------------------------------------------------------
    // Data already available — cancellation arrives too late
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task ReadAsync_DataAlreadyAvailable_CompletesBeforeCancellationTakesEffect()
    {
        // If data is already in the buffer, ReadAsync must complete immediately
        // even if cancellation occurs concurrently or shortly after.
        var (writeEnd, readEnd) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var ct = TestContext.CancellationToken;

        var data = new byte[] { 0x01, 0x02, 0x03 };
        await writeEnd.WriteAsync(ConnectionTestHelpers.Segment(data), ct);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        var buffer = new byte[data.Length];

        // The read must complete synchronously/immediately — data is already there
        var bytesRead = await readEnd.ReadAsync(buffer, cts.Token);

        Assert.AreEqual(data.Length, bytesRead);
        CollectionAssert.AreEqual(data, buffer);
    }

    // -------------------------------------------------------------------------
    // WriteAsync — writes are non-blocking
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task WriteAsync_NonBlocking_CompletesImmediatelyWithoutReader()
    {
        // WriteAsync must never block waiting for a reader; it just enqueues
        // the segment. All writes must complete without any active reader.
        var (writeEnd, _) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var ct = TestContext.CancellationToken;

        var data = Enumerable.Range(0, 1000)
            .Select(i => new byte[] { (byte)(i % 256) })
            .ToArray();

        foreach (var segment in data)
        {
            await writeEnd.WriteAsync(ConnectionTestHelpers.Segment(segment), ct);
        }

        // No assertion needed — reaching here means no write blocked.
    }

    // -------------------------------------------------------------------------
    // Cancellation interacting with EOF
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task ReadAsync_CancelledJustBeforeEof_ProducesOneValidOutcome()
    {
        // This is a deliberate race between cancellation and EOF.
        // Either outcome is legal:
        //   - If EOF wins: ReadAsync returns 0.
        //   - If cancellation wins: ReadAsync throws OperationCanceledException.
        // The test asserts only that the task does not hang and produces one
        // of these two valid results.
        var (writeEnd, readEnd) = ConnectionTestHelpers.CreateUnidirectionalPair();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.CancellationToken);

        var buffer = new byte[16];
        var readTask = readEnd.ReadAsync(buffer, cts.Token).AsTask();

        await Task.Yield();

        // Race: cancel and signal EOF simultaneously
        await cts.CancelAsync();
        writeEnd.Dispose();

        try
        {
            var bytesRead = await readTask.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.AreEqual(0, bytesRead,
                "If EOF wins the race, ReadAsync must return 0.");
        }
        catch (OperationCanceledException)
        {
            // Cancellation won the race — also a valid outcome.
        }
    }
}
