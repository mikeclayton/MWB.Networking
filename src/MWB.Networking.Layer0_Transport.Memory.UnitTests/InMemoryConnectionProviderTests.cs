using MWB.Networking.Layer0_Transport.Memory.Buffer;
using MWB.Networking.Layer0_Transport.Memory.UnitTests.Helpers;
using MWB.Networking.Layer0_Transport.Stack;

namespace MWB.Networking.Layer0_Transport.Memory.UnitTests;

[TestClass]
public sealed class InMemoryConnectionProviderTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Constructor_WithNullBuffer_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new InMemoryNetworkConnectionProvider(null!, SegmentedDuplexBufferSide.SideA));
    }

    [TestMethod]
    public void Constructor_ValidArguments_DoesNotThrow()
    {
        var buffer = new SegmentedDuplexBuffer();

        var providerA = new InMemoryNetworkConnectionProvider(buffer, SegmentedDuplexBufferSide.SideA);
        var providerB = new InMemoryNetworkConnectionProvider(buffer, SegmentedDuplexBufferSide.SideB);

        Assert.IsNotNull(providerA);
        Assert.IsNotNull(providerB);
    }

    // -------------------------------------------------------------------------
    // OpenConnectionAsync — happy path
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task OpenConnectionAsync_ReturnsNonNullConnection()
    {
        var (providerA, _) = ConnectionTestHelpers.CreateDuplexProviders();

        var conn = await providerA.OpenConnectionAsync(
            new ObservableConnectionStatus(), TestContext.CancellationToken);

        Assert.IsNotNull(conn);
    }

    [TestMethod]
    public async Task OpenConnectionAsync_ConnectionIsInConnectedState()
    {
        // OpenConnectionAsync binds the status and immediately transitions it
        // through Connecting → Connected. The status must be Connected on return.
        var (providerA, _) = ConnectionTestHelpers.CreateDuplexProviders();
        var status = new ObservableConnectionStatus();

        await providerA.OpenConnectionAsync(status, TestContext.CancellationToken);

        Assert.AreEqual(TransportConnectionState.Connected, status.State);
    }

    [TestMethod]
    public async Task OpenConnectionAsync_RaisesConnectingThenConnectedEvents()
    {
        var (providerA, _) = ConnectionTestHelpers.CreateDuplexProviders();
        var status = new ObservableConnectionStatus();
        var events = new List<string>();

        status.Connecting += (_, _) => events.Add("Connecting");
        status.Connected += (_, _) => events.Add("Connected");

        await providerA.OpenConnectionAsync(status, TestContext.CancellationToken);

        CollectionAssert.AreEqual(
            new[] { "Connecting", "Connected" },
            events);
    }

    [TestMethod]
    public async Task OpenConnectionAsync_BothSides_ReturnDistinctConnections()
    {
        var (providerA, providerB) = ConnectionTestHelpers.CreateDuplexProviders();
        var ct = TestContext.CancellationToken;

        var (connA, connB) = await ConnectionTestHelpers.OpenBothAsync(providerA, providerB, ct);

        Assert.AreNotSame(connA, connB,
            "SideA and SideB must expose different logical connection objects.");
    }

    // -------------------------------------------------------------------------
    // OpenConnectionAsync — error cases
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task OpenConnectionAsync_CalledTwice_ThrowsInvalidOperationException()
    {
        var (providerA, _) = ConnectionTestHelpers.CreateDuplexProviders();
        var ct = TestContext.CancellationToken;

        await providerA.OpenConnectionAsync(new ObservableConnectionStatus(), ct);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await providerA.OpenConnectionAsync(new ObservableConnectionStatus(), ct));
    }

    [TestMethod]
    public async Task OpenConnectionAsync_WithAlreadyCancelledToken_ThrowsOperationCanceledException()
    {
        var (providerA, _) = ConnectionTestHelpers.CreateDuplexProviders();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            async () => await providerA.OpenConnectionAsync(new ObservableConnectionStatus(), cts.Token));
    }

    [TestMethod]
    public async Task OpenConnectionAsync_CalledOncePerSide_BothSucceed()
    {
        // Each side has its own "opened" guard; opening SideA once and SideB once is legal.
        var (providerA, providerB) = ConnectionTestHelpers.CreateDuplexProviders();
        var ct = TestContext.CancellationToken;

        var connA = await providerA.OpenConnectionAsync(new ObservableConnectionStatus(), ct);
        var connB = await providerB.OpenConnectionAsync(new ObservableConnectionStatus(), ct);

        Assert.IsNotNull(connA);
        Assert.IsNotNull(connB);
    }

    // -------------------------------------------------------------------------
    // End-to-end duplex communication through the provider
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task DuplexProviders_AtoB_DataDeliveredToB()
    {
        var (providerA, providerB) = ConnectionTestHelpers.CreateDuplexProviders();
        var ct = TestContext.CancellationToken;

        var (connA, connB) = await ConnectionTestHelpers.OpenBothAsync(providerA, providerB, ct);

        var data = new byte[] { 0x01, 0x02, 0x03 };

        await connA.WriteAsync(ConnectionTestHelpers.Segment(data), ct);
        connA.Dispose(); // signal EOF in A→B direction

        var received = await ConnectionTestHelpers
            .ReadToEndAsync(connB, ct)
            .WaitAsync(TimeSpan.FromSeconds(5), ct);

        CollectionAssert.AreEqual(data, received);
    }

    [TestMethod]
    public async Task DuplexProviders_BtoA_DataDeliveredToA()
    {
        var (providerA, providerB) = ConnectionTestHelpers.CreateDuplexProviders();
        var ct = TestContext.CancellationToken;

        var (connA, connB) = await ConnectionTestHelpers.OpenBothAsync(providerA, providerB, ct);

        var data = new byte[] { 0xAA, 0xBB, 0xCC };

        await connB.WriteAsync(ConnectionTestHelpers.Segment(data), ct);
        connB.Dispose(); // signal EOF in B→A direction

        var received = await ConnectionTestHelpers
            .ReadToEndAsync(connA, ct)
            .WaitAsync(TimeSpan.FromSeconds(5), ct);

        CollectionAssert.AreEqual(data, received);
    }

    [TestMethod]
    public async Task DuplexProviders_FullDuplex_BothSidesReceiveCorrectData()
    {
        // Uses ReadExactAsync (known byte count) to avoid needing an EOF signal
        // mid-test, so neither connection needs to be disposed early.
        var (providerA, providerB) = ConnectionTestHelpers.CreateDuplexProviders();
        var ct = TestContext.CancellationToken;

        var (connA, connB) = await ConnectionTestHelpers.OpenBothAsync(providerA, providerB, ct);

        var dataAtoB = new byte[] { 0x01, 0x02, 0x03 };
        var dataBtoA = new byte[] { 0xAA, 0xBB, 0xCC };

        // Write in both directions concurrently
        var writeAtoB = connA.WriteAsync(ConnectionTestHelpers.Segment(dataAtoB), ct).AsTask();
        var writeBtoA = connB.WriteAsync(ConnectionTestHelpers.Segment(dataBtoA), ct).AsTask();

        await Task.WhenAll(writeAtoB, writeBtoA);

        // Read exact byte counts concurrently from each side
        var readByB = ConnectionTestHelpers.ReadExactAsync(connB, dataAtoB.Length, ct);
        var readByA = ConnectionTestHelpers.ReadExactAsync(connA, dataBtoA.Length, ct);

        var receivedByB = await readByB.WaitAsync(TimeSpan.FromSeconds(5), ct);
        var receivedByA = await readByA.WaitAsync(TimeSpan.FromSeconds(5), ct);

        CollectionAssert.AreEqual(dataAtoB, receivedByB,
            "B should receive exactly what A wrote.");
        CollectionAssert.AreEqual(dataBtoA, receivedByA,
            "A should receive exactly what B wrote.");
    }

    [TestMethod]
    public async Task DuplexProviders_SequentialExchange_RoundTrip()
    {
        // Simple ping-pong: A sends, B receives and replies, A receives reply.
        var (providerA, providerB) = ConnectionTestHelpers.CreateDuplexProviders();
        var ct = TestContext.CancellationToken;

        var (connA, connB) = await ConnectionTestHelpers.OpenBothAsync(providerA, providerB, ct);

        var ping = new byte[] { 0x50, 0x49, 0x4E, 0x47 }; // "PING"
        var pong = new byte[] { 0x50, 0x4F, 0x4E, 0x47 }; // "PONG"

        // A sends ping to B
        await connA.WriteAsync(ConnectionTestHelpers.Segment(ping), ct);

        // B reads ping (exact count — no EOF signal yet)
        var receivedPing = await ConnectionTestHelpers
            .ReadExactAsync(connB, ping.Length, ct)
            .WaitAsync(TimeSpan.FromSeconds(5), ct);

        CollectionAssert.AreEqual(ping, receivedPing);

        // B sends pong back to A
        await connB.WriteAsync(ConnectionTestHelpers.Segment(pong), ct);

        // A reads pong
        var receivedPong = await ConnectionTestHelpers
            .ReadExactAsync(connA, pong.Length, ct)
            .WaitAsync(TimeSpan.FromSeconds(5), ct);

        CollectionAssert.AreEqual(pong, receivedPong);
    }

    // -------------------------------------------------------------------------
    // Status is updated per-side independently
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task OpenConnectionAsync_EachSideHasIndependentStatus()
    {
        var (providerA, providerB) = ConnectionTestHelpers.CreateDuplexProviders();
        var ct = TestContext.CancellationToken;

        var statusA = new ObservableConnectionStatus();
        var statusB = new ObservableConnectionStatus();

        var connA = await providerA.OpenConnectionAsync(statusA, ct);
        var connB = await providerB.OpenConnectionAsync(statusB, ct);

        Assert.AreEqual(TransportConnectionState.Connected, statusA.State);
        Assert.AreEqual(TransportConnectionState.Connected, statusB.State);

        connA.Dispose();

        Assert.AreEqual(TransportConnectionState.Disconnected, statusA.State,
            "Disposing connA must only affect statusA.");
        Assert.AreEqual(TransportConnectionState.Connected, statusB.State,
            "Disposing connA must not affect statusB.");
    }

    // -------------------------------------------------------------------------
    // Provider disposal
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Dispose_IsIdempotentAndDoesNotThrow()
    {
        var (providerA, providerB) = ConnectionTestHelpers.CreateDuplexProviders();

        providerA.Dispose();
        providerA.Dispose();
        providerB.Dispose();
    }

    [TestMethod]
    public async Task Dispose_AfterOpenConnection_DoesNotThrow()
    {
        var (providerA, _) = ConnectionTestHelpers.CreateDuplexProviders();

        await providerA.OpenConnectionAsync(
            new ObservableConnectionStatus(), TestContext.CancellationToken);

        providerA.Dispose(); // provider is lifecycle-agnostic — must be a no-op
    }
}
