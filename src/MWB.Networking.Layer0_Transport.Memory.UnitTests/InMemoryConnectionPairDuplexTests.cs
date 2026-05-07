using MWB.Networking.Layer0_Transport.Memory.UnitTests.Helpers;

namespace MWB.Networking.Layer0_Transport.Memory.UnitTests;

/// <summary>
/// Tests for bidirectional (duplex) behaviour of a cross-wired
/// <see cref="InMemoryNetworkConnection"/> pair backed by a
/// <see cref="MWB.Networking.Layer0_Transport.Memory.Buffer.SegmentedDuplexBuffer"/>.
///
/// The pair creates two independent unidirectional byte channels:
///   A→B: connectionA.WriteAsync → connectionB.ReadAsync
///   B→A: connectionB.WriteAsync → connectionA.ReadAsync
///
/// Each direction is independent — activity or disposal in one direction
/// must not affect the other.
/// </summary>
[TestClass]
public sealed class InMemoryConnectionPairDuplexTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    // -------------------------------------------------------------------------
    // Happy path: each direction independently
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task AWritesToB_BReceivesData()
    {
        var (connectionA, connectionB) = ConnectionTestHelpers.CreateDuplexInMemoryConnectionPair();
        var ct = TestContext.CancellationToken;

        var data = new byte[] { 0x01, 0x02, 0x03 };
        await connectionA.WriteAsync(ConnectionTestHelpers.Segment(data), ct);
        connectionA.Dispose(); // signal EOF in the A→B direction

        var received = await ConnectionTestHelpers.ReadToEndAsync(connectionB, ct);

        CollectionAssert.AreEqual(data, received);
    }

    [TestMethod]
    public async Task BWritesToA_AReceivesData()
    {
        var (connectionA, connectionB) = ConnectionTestHelpers.CreateDuplexInMemoryConnectionPair();
        var ct = TestContext.CancellationToken;

        var data = new byte[] { 0xAA, 0xBB, 0xCC };
        await connectionB.WriteAsync(ConnectionTestHelpers.Segment(data), ct);
        connectionB.Dispose(); // signal EOF in the B→A direction

        var received = await ConnectionTestHelpers.ReadToEndAsync(connectionA, ct);

        CollectionAssert.AreEqual(data, received);
    }

    // -------------------------------------------------------------------------
    // Full-duplex: both directions active at the same time
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task FullDuplex_SimultaneousWrites_BothSidesReceiveCorrectData()
    {
        // Uses ReadExactAsync (known byte count) so neither connection needs to be
        // disposed before reading — disposing a connection prevents further ReadAsync
        // calls on that same endpoint in the new design.
        var (connectionA, connectionB) = ConnectionTestHelpers.CreateDuplexInMemoryConnectionPair();
        var ct = TestContext.CancellationToken;

        var dataAtoB = new byte[] { 0x01, 0x02, 0x03 };
        var dataBtoA = new byte[] { 0xAA, 0xBB, 0xCC };

        // Write concurrently in both directions
        var writeAtoB = connectionA.WriteAsync(
            ConnectionTestHelpers.Segment(dataAtoB), ct).AsTask();
        var writeBtoA = connectionB.WriteAsync(
            ConnectionTestHelpers.Segment(dataBtoA), ct).AsTask();

        await Task.WhenAll(writeAtoB, writeBtoA);

        // Read exact byte counts — no EOF signal needed
        var readByB = ConnectionTestHelpers.ReadExactAsync(connectionB, dataAtoB.Length, ct);
        var readByA = ConnectionTestHelpers.ReadExactAsync(connectionA, dataBtoA.Length, ct);

        var receivedByB = await readByB.WaitAsync(TimeSpan.FromSeconds(5), ct);
        var receivedByA = await readByA.WaitAsync(TimeSpan.FromSeconds(5), ct);

        CollectionAssert.AreEqual(dataAtoB, receivedByB,
            "B should receive exactly what A wrote.");
        CollectionAssert.AreEqual(dataBtoA, receivedByA,
            "A should receive exactly what B wrote.");
    }

    [TestMethod]
    public async Task FullDuplex_ConcurrentStreamingInBothDirections_NoDataCorruption()
    {
        // Start reading before writing so reads and writes proceed concurrently.
        // Uses ReadExactAsync (known total byte count) — disposing a connection
        // prevents further ReadAsync on that same endpoint in the new design.
        var (connectionA, connectionB) = ConnectionTestHelpers.CreateDuplexInMemoryConnectionPair();
        var ct = TestContext.CancellationToken;

        const int messageCount = 500;
        const int bytesPerMessage = 2;
        const int totalBytes = messageCount * bytesPerMessage;

        var sentAtoB = Enumerable.Range(0, messageCount)
            .Select(i => new byte[] { (byte)(i % 256), (byte)((i >> 8) % 256) })
            .ToList();

        var sentBtoA = Enumerable.Range(0, messageCount)
            .Select(i => new byte[] { (byte)((i + 128) % 256), (byte)(((i + 128) >> 8) % 256) })
            .ToList();

        // Start reading concurrently with the writes, using exact total counts
        var readByB = ConnectionTestHelpers.ReadExactAsync(connectionB, totalBytes, ct);
        var readByA = ConnectionTestHelpers.ReadExactAsync(connectionA, totalBytes, ct);

        // Stream writes in both directions simultaneously
        var writeAtoB = Task.Run(async () =>
        {
            foreach (var msg in sentAtoB)
                await connectionA.WriteAsync(ConnectionTestHelpers.Segment(msg), ct);
        }, ct);

        var writeBtoA = Task.Run(async () =>
        {
            foreach (var msg in sentBtoA)
                await connectionB.WriteAsync(ConnectionTestHelpers.Segment(msg), ct);
        }, ct);

        await Task.WhenAll(writeAtoB, writeBtoA).WaitAsync(TimeSpan.FromSeconds(10), ct);
        var receivedByB = await readByB.WaitAsync(TimeSpan.FromSeconds(10), ct);
        var receivedByA = await readByA.WaitAsync(TimeSpan.FromSeconds(10), ct);

        CollectionAssert.AreEqual(
            sentAtoB.SelectMany(m => m).ToArray(),
            receivedByB,
            "B should receive A's data with no corruption or reordering.");

        CollectionAssert.AreEqual(
            sentBtoA.SelectMany(m => m).ToArray(),
            receivedByA,
            "A should receive B's data with no corruption or reordering.");
    }

    // -------------------------------------------------------------------------
    // Direction independence: disposing one side must not affect the other
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task DisposeOfA_DoesNotSignalEofInBtoADirection()
    {
        // Disposing connectionA completes only the A→B buffer.
        // connectionB must still be able to write in the B→A direction.
        var (connectionA, connectionB) = ConnectionTestHelpers.CreateDuplexInMemoryConnectionPair();
        var ct = TestContext.CancellationToken;

        // Dispose A — signals EOF in A→B; B's reader will see 0
        connectionA.Dispose();

        // B→A direction is unaffected: B can still write without error
        var data = new byte[] { 0xDE, 0xAD };
        await connectionB.WriteAsync(ConnectionTestHelpers.Segment(data), ct);

        // B's read side (reads from A→B) should see EOF immediately
        var bReadBuffer = new byte[16];
        var bRead = await connectionB.ReadAsync(bReadBuffer, ct);
        Assert.AreEqual(0, bRead,
            "B's reader should see EOF because A disposed its write end.");
    }

    [TestMethod]
    public async Task DisposeOfB_DoesNotSignalEofInAtoBDirection()
    {
        // Disposing connectionB completes only the B→A buffer.
        // connectionA must still be able to write in the A→B direction.
        var (connectionA, connectionB) = ConnectionTestHelpers.CreateDuplexInMemoryConnectionPair();
        var ct = TestContext.CancellationToken;

        connectionB.Dispose();

        // A→B direction remains intact
        var data = new byte[] { 0x42 };
        await connectionA.WriteAsync(ConnectionTestHelpers.Segment(data), ct);

        // A's read side (reads from B→A) should see EOF
        var aReadBuffer = new byte[16];
        var aRead = await connectionA.ReadAsync(aReadBuffer, ct);
        Assert.AreEqual(0, aRead,
            "A's reader should see EOF because B disposed its write end.");
    }

    // -------------------------------------------------------------------------
    // Message ordering: many messages, strict delivery order
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task ManySequentialMessages_DeliveredInWriteOrder()
    {
        var (connectionA, connectionB) = ConnectionTestHelpers.CreateDuplexInMemoryConnectionPair();
        var ct = TestContext.CancellationToken;

        const int messageCount = 1000;

        // Write bytes 0..255 repeatedly as individual single-byte messages
        var expected = Enumerable.Range(0, messageCount)
            .Select(i => (byte)(i % 256))
            .ToArray();

        foreach (var b in expected)
            await connectionA.WriteAsync(ConnectionTestHelpers.Segment(b), ct);

        connectionA.Dispose();

        var received = await ConnectionTestHelpers
            .ReadToEndAsync(connectionB, ct)
            .WaitAsync(TimeSpan.FromSeconds(10), ct);

        CollectionAssert.AreEqual(expected, received,
            "Messages must be delivered in write order.");
    }
}
