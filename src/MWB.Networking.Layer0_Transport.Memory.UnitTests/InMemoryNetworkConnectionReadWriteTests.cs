using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer0_Transport.Memory.UnitTests.Helpers;
using System.Diagnostics;

namespace MWB.Networking.Layer0_Transport.Memory.UnitTests;

[TestClass]
public sealed class InMemoryNetworkConnectionReadWriteTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    // -------------------------------------------------------------------------
    // Single write / single read
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task WriteAsync_SingleSegment_ReadsBackIdenticalBytes()
    {
        var (writeEnd, readEnd) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var ct = TestContext.CancellationToken;
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

        await writeEnd.WriteAsync(ConnectionTestHelpers.Segment(data), ct);
        writeEnd.Dispose();

        var received = await ConnectionTestHelpers.ReadToEndAsync(readEnd, ct);

        CollectionAssert.AreEqual(data, received);
    }

    [TestMethod]
    public async Task WriteAsync_EmptyPayload_DoesNotCorruptStream()
    {
        // An empty write is a no-op; the subsequent non-empty write should
        // still be delivered in full.
        var (writeEnd, readEnd) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var ct = TestContext.CancellationToken;

        await writeEnd.WriteAsync(ConnectionTestHelpers.Segment([]), ct);
        await writeEnd.WriteAsync(ConnectionTestHelpers.Segment(0x42), ct);
        writeEnd.Dispose();

        var received = await ConnectionTestHelpers.ReadToEndAsync(readEnd, ct);

        CollectionAssert.AreEqual(new byte[] { 0x42 }, received);
    }

    [TestMethod]
    public async Task WriteAsync_LargePayload_FullyReadable()
    {
        var (writeEnd, readEnd) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var ct = TestContext.CancellationToken;

        var data = new byte[1024 * 1024 * 100];
        new Random(42).NextBytes(data);

        var writerStopwatch = Stopwatch.StartNew();
        await writeEnd.WriteAsync(ConnectionTestHelpers.Segment(data), ct);
        writerStopwatch.Stop();
        writeEnd.Dispose();

        TestContext.WriteLine(
            $"Write completed in {writerStopwatch.ElapsedMilliseconds} ms " +
            $"({Throughput(data.Length, writerStopwatch)} MB/s)");

        var readerStopwatch = Stopwatch.StartNew();
        var received = await ConnectionTestHelpers.ReadToEndAsync(readEnd, ct);
        readerStopwatch.Stop();

        TestContext.WriteLine(
            $"Read completed in {readerStopwatch.ElapsedMilliseconds} ms " +
            $"({Throughput(received.Length, readerStopwatch)} MB/s)");

        CollectionAssert.AreEqual(data, received);
    }

    private static double Throughput(long bytes, Stopwatch sw)
    {
        if (sw.Elapsed.TotalSeconds == 0)
            return double.PositiveInfinity;

        var megabytes = bytes / (1024d * 1024d);
        return megabytes / sw.Elapsed.TotalSeconds;
    }

    // -------------------------------------------------------------------------
    // Multiple writes
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task WriteAsync_MultipleWrites_ReadsInWriteOrder()
    {
        var (writeEnd, readEnd) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var ct = TestContext.CancellationToken;

        var seg1 = new byte[] { 0x01, 0x02, 0x03 };
        var seg2 = new byte[] { 0x04, 0x05, 0x06 };
        var seg3 = new byte[] { 0x07, 0x08, 0x09 };

        await writeEnd.WriteAsync(ConnectionTestHelpers.Segment(seg1), ct);
        await writeEnd.WriteAsync(ConnectionTestHelpers.Segment(seg2), ct);
        await writeEnd.WriteAsync(ConnectionTestHelpers.Segment(seg3), ct);
        writeEnd.Dispose();

        var received = await ConnectionTestHelpers.ReadToEndAsync(readEnd, ct);

        CollectionAssert.AreEqual(
            seg1.Concat(seg2).Concat(seg3).ToArray(),
            received);
    }

    [TestMethod]
    public async Task WriteAsync_ByteSegmentsWithMultipleSegments_AllDeliveredInOrder()
    {
        // A single ByteSegments containing multiple internal segments must behave
        // identically to multiple individual writes.
        var (writeEnd, readEnd) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var ct = TestContext.CancellationToken;

        var seg1 = new byte[] { 0x0A, 0x0B };
        var seg2 = new byte[] { 0x0C, 0x0D };
        var seg3 = new byte[] { 0x0E, 0x0F };

        await writeEnd.WriteAsync(new ByteSegments(seg1, seg2, seg3), ct);
        writeEnd.Dispose();

        var received = await ConnectionTestHelpers.ReadToEndAsync(readEnd, ct);

        CollectionAssert.AreEqual(
            new byte[] { 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F },
            received);
    }

    // -------------------------------------------------------------------------
    // Partial reads (stream semantics — ReadAsync may return fewer bytes)
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task ReadAsync_DestinationSmallerThanSegment_ReturnsOnlyRequestedBytes()
    {
        var (writeEnd, readEnd) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var ct = TestContext.CancellationToken;

        await writeEnd.WriteAsync(
            ConnectionTestHelpers.Segment(0x01, 0x02, 0x03, 0x04, 0x05), ct);

        // Destination only fits 3 bytes
        var smallBuffer = new byte[3];
        var bytesRead = await readEnd.ReadAsync(smallBuffer, ct);

        Assert.AreEqual(3, bytesRead);
        CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03 }, smallBuffer);
    }

    [TestMethod]
    public async Task ReadAsync_DestinationLargerThanSegment_ReturnsOnlyAvailableBytes()
    {
        var (writeEnd, readEnd) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var ct = TestContext.CancellationToken;

        await writeEnd.WriteAsync(
            ConnectionTestHelpers.Segment(0x11, 0x22, 0x33), ct);

        // Destination is bigger than the segment — only the segment's bytes are returned
        var largeBuffer = new byte[1024];
        var bytesRead = await readEnd.ReadAsync(largeBuffer, ct);

        Assert.AreEqual(3, bytesRead);
        CollectionAssert.AreEqual(
            new byte[] { 0x11, 0x22, 0x33 },
            largeBuffer[..3]);
    }

    [TestMethod]
    public async Task ReadAsync_MultipleReadsFromSingleWrite_ReassemblesCompletePayload()
    {
        var (writeEnd, readEnd) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var ct = TestContext.CancellationToken;

        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };
        await writeEnd.WriteAsync(ConnectionTestHelpers.Segment(data), ct);
        writeEnd.Dispose();

        // Read in 2-byte chunks and reassemble
        var buffer = new byte[2];
        var allBytes = new List<byte>();

        int bytesRead;
        while ((bytesRead = await readEnd.ReadAsync(buffer, ct)) > 0)
        {
            allBytes.AddRange(buffer[..bytesRead]);
        }

        CollectionAssert.AreEqual(data, allBytes.ToArray());
    }

    [TestMethod]
    public async Task ReadAsync_RemainingBytesAfterPartialRead_AreAvailableOnNextRead()
    {
        var (writeEnd, readEnd) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var ct = TestContext.CancellationToken;

        await writeEnd.WriteAsync(
            ConnectionTestHelpers.Segment(0xAA, 0xBB, 0xCC, 0xDD), ct);

        // First read: consume only 2 bytes
        var firstBuffer = new byte[2];
        var firstRead = await readEnd.ReadAsync(firstBuffer, ct);
        Assert.AreEqual(2, firstRead);
        CollectionAssert.AreEqual(new byte[] { 0xAA, 0xBB }, firstBuffer);

        // Second read: the remaining 2 bytes must still be available
        var secondBuffer = new byte[2];
        var secondRead = await readEnd.ReadAsync(secondBuffer, ct);
        Assert.AreEqual(2, secondRead);
        CollectionAssert.AreEqual(new byte[] { 0xCC, 0xDD }, secondBuffer);
    }

    // -------------------------------------------------------------------------
    // Blocking reads (reader waits for writer)
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task ReadAsync_BlocksUntilWriterProducesData()
    {
        var (writeEnd, readEnd) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var ct = TestContext.CancellationToken;

        var buffer = new byte[1];

        // Start a read before any data is written — should block
        var readTask = readEnd.ReadAsync(buffer, ct).AsTask();

        await Task.Yield();
        Assert.IsFalse(readTask.IsCompleted,
            "ReadAsync should be blocked waiting for data.");

        // Now produce the data
        await writeEnd.WriteAsync(ConnectionTestHelpers.Segment(0xAB), ct);

        var bytesRead = await readTask.WaitAsync(TimeSpan.FromSeconds(5), ct);

        Assert.AreEqual(1, bytesRead);
        Assert.AreEqual(0xAB, buffer[0]);
    }

    [TestMethod]
    public async Task ReadAsync_DataArrivesAfterDelay_IsDeliveredCorrectly()
    {
        var (writeEnd, readEnd) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var ct = TestContext.CancellationToken;

        var expected = new byte[] { 0x10, 0x20, 0x30 };

        // Write from a background task with a short delay
        var writeTask = Task.Run(async () =>
        {
            await Task.Delay(50, ct);
            await writeEnd.WriteAsync(ConnectionTestHelpers.Segment(expected), ct);
            writeEnd.Dispose();
        }, ct);

        var received = await ConnectionTestHelpers
            .ReadToEndAsync(readEnd, ct)
            .WaitAsync(TimeSpan.FromSeconds(5), ct);

        await writeTask;

        CollectionAssert.AreEqual(expected, received);
    }
}
