namespace MWB.Networking.Layer2_Protocol.Adapter.UnitTests;

/// <summary>
/// Tests for <see cref="RingBuffer{T}"/>.
///
/// Contract under test:
/// - Zero or negative capacity is rejected at construction.
/// - <see cref="RingBuffer{T}.Snapshot"/> on an empty buffer returns an empty array.
/// - Written items appear in the snapshot in chronological (FIFO) order.
/// - When capacity is exceeded, the oldest entries are overwritten and only the
///   most recent <c>capacity</c> items appear in the snapshot.
/// - <see cref="RingBuffer{T}.Snapshot"/> is non-destructive: repeated calls
///   return the same logical sequence.
/// - Concurrent writes do not corrupt the buffer (structural safety).
/// - <see cref="RingBuffer{T}.Snapshot"/> allocates a fresh array every call;
///   mutations to the returned array do not affect the buffer.
/// </summary>
[TestClass]
public sealed class RingBuffer_Tests
{
    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    // -----------------------------------------------------------------------
    // Constructor validation
    // -----------------------------------------------------------------------

    [TestMethod]
    public void Constructor_ZeroCapacity_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RingBuffer<int>(0));
    }

    [TestMethod]
    public void Constructor_NegativeCapacity_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RingBuffer<int>(-1));
    }

    [TestMethod]
    public void Constructor_PositiveCapacity_DoesNotThrow()
    {
        var _ = new RingBuffer<int>(1);
    }

    // -----------------------------------------------------------------------
    // Empty buffer
    // -----------------------------------------------------------------------

    [TestMethod]
    public void Snapshot_EmptyBuffer_ReturnsEmptyArray()
    {
        var buf = new RingBuffer<int>(4);

        Assert.AreEqual(0, buf.Snapshot().Length);
    }

    // -----------------------------------------------------------------------
    // Write and read back (capacity not exceeded)
    // -----------------------------------------------------------------------

    [TestMethod]
    public void SingleWrite_AppearsInSnapshot()
    {
        var buf = new RingBuffer<int>(4);
        buf.Write(42);

        var snap = buf.Snapshot();

        Assert.AreEqual(1, snap.Length);
        Assert.AreEqual(42, snap[0]);
    }

    [TestMethod]
    public void MultipleWrites_AppearsInChronologicalOrder()
    {
        var buf = new RingBuffer<int>(8);
        buf.Write(1);
        buf.Write(2);
        buf.Write(3);

        var snap = buf.Snapshot();

        Assert.AreEqual(3, snap.Length);
        Assert.AreEqual(1, snap[0]);
        Assert.AreEqual(2, snap[1]);
        Assert.AreEqual(3, snap[2]);
    }

    [TestMethod]
    public void WriteExactlyAtCapacity_AllItemsRetained()
    {
        var buf = new RingBuffer<int>(3);
        buf.Write(10);
        buf.Write(20);
        buf.Write(30);

        var snap = buf.Snapshot();

        Assert.AreEqual(3, snap.Length);
        Assert.AreEqual(10, snap[0]);
        Assert.AreEqual(20, snap[1]);
        Assert.AreEqual(30, snap[2]);
    }

    // -----------------------------------------------------------------------
    // Overwrite behaviour (capacity exceeded)
    // -----------------------------------------------------------------------

    [TestMethod]
    public void WriteOneMoreThanCapacity_OldestEntryIsDropped()
    {
        var buf = new RingBuffer<int>(3);
        buf.Write(1);
        buf.Write(2);
        buf.Write(3);
        buf.Write(4); // overflows: 1 is lost

        var snap = buf.Snapshot();

        Assert.AreEqual(3, snap.Length);
        Assert.AreEqual(2, snap[0]);
        Assert.AreEqual(3, snap[1]);
        Assert.AreEqual(4, snap[2]);
    }

    [TestMethod]
    public void WriteManyMoreThanCapacity_OnlyMostRecentItemsRetained()
    {
        var buf = new RingBuffer<int>(4);
        for (var i = 1; i <= 20; i++)
            buf.Write(i);

        var snap = buf.Snapshot();

        Assert.AreEqual(4, snap.Length);
        Assert.AreEqual(17, snap[0]);
        Assert.AreEqual(18, snap[1]);
        Assert.AreEqual(19, snap[2]);
        Assert.AreEqual(20, snap[3]);
    }

    [TestMethod]
    public void Snapshot_AfterOverflow_IsInChronologicalOrder()
    {
        // Verifies oldest-first ordering is maintained even after the write
        // pointer has wrapped around multiple times.
        var buf = new RingBuffer<int>(3);
        for (var i = 1; i <= 10; i++)
            buf.Write(i);

        // Last 3 written: 8, 9, 10
        var snap = buf.Snapshot();

        Assert.AreEqual(8, snap[0]);
        Assert.AreEqual(9, snap[1]);
        Assert.AreEqual(10, snap[2]);
    }

    // -----------------------------------------------------------------------
    // Snapshot is non-destructive
    // -----------------------------------------------------------------------

    [TestMethod]
    public void Snapshot_CalledTwice_ReturnsSameContent()
    {
        var buf = new RingBuffer<int>(4);
        buf.Write(1);
        buf.Write(2);
        buf.Write(3);

        var snap1 = buf.Snapshot();
        var snap2 = buf.Snapshot();

        CollectionAssert.AreEqual(snap1, snap2);
    }

    [TestMethod]
    public void Snapshot_ReturnsFreshArray_MutationDoesNotAffectBuffer()
    {
        var buf = new RingBuffer<int>(4);
        buf.Write(10);

        var snap1 = buf.Snapshot();
        snap1[0] = 999; // mutate the returned array

        var snap2 = buf.Snapshot();
        Assert.AreEqual(10, snap2[0]); // buffer must be unaffected
    }

    // -----------------------------------------------------------------------
    // Capacity-1 edge case
    // -----------------------------------------------------------------------

    [TestMethod]
    public void CapacityOne_AlwaysHoldsOnlyMostRecentWrite()
    {
        var buf = new RingBuffer<int>(1);
        buf.Write(1);
        buf.Write(2);
        buf.Write(3);

        var snap = buf.Snapshot();

        Assert.AreEqual(1, snap.Length);
        Assert.AreEqual(3, snap[0]);
    }

    // -----------------------------------------------------------------------
    // Works with reference types
    // -----------------------------------------------------------------------

    [TestMethod]
    public void ReferenceType_WrittenAndReadBack()
    {
        var buf = new RingBuffer<string>(4);
        buf.Write("alpha");
        buf.Write("beta");

        var snap = buf.Snapshot();

        Assert.AreEqual("alpha", snap[0]);
        Assert.AreEqual("beta",  snap[1]);
    }

    // -----------------------------------------------------------------------
    // Concurrent writes — structural safety
    // -----------------------------------------------------------------------

    [TestMethod]
    public void ConcurrentWrites_DoNotCorruptBuffer()
    {
        // This test verifies that the lock-free write path does not corrupt
        // the buffer under concurrent load. It does not enforce ordering (which
        // is not guaranteed across threads) but does verify:
        //   - No exceptions are thrown.
        //   - The snapshot length equals capacity after saturation.
        //   - Every value in the snapshot is within the expected range.
        const int Capacity = 16;
        const int Writers = 8;
        const int WritesPerThread = 1000;

        var buf = new RingBuffer<int>(Capacity);

        var threads = Enumerable.Range(0, Writers)
            .Select(t => new Thread(() =>
            {
                for (var i = 0; i < WritesPerThread; i++)
                    buf.Write(t * WritesPerThread + i);
            }))
            .ToList();

        foreach (var t in threads) t.Start();
        foreach (var t in threads) t.Join();

        var snap = buf.Snapshot();

        Assert.AreEqual(Capacity, snap.Length);

        var maxExpected = Writers * WritesPerThread - 1;
        foreach (var v in snap)
            Assert.IsTrue(v >= 0 && v <= maxExpected,
                $"Unexpected value {v} in snapshot — buffer may be corrupted.");
    }
}
