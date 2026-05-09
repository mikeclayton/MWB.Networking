using MWB.Networking.Layer0_Transport.Stack.Lifecycle;
using MWB.Networking.Layer0_Transport.Memory.UnitTests.Helpers;
using MWB.Networking.Layer0_Transport.Stack.Core.Lifecycle;

namespace MWB.Networking.Layer0_Transport.Memory.UnitTests;

/// <summary>
/// Tests for <see cref="InMemoryNetworkConnection"/> lifecycle semantics:
/// EOF signalling, dispose behaviour, direction isolation, and
/// <see cref="ObservableConnectionStatus"/> integration.
/// </summary>
[TestClass]
public sealed class InMemoryNetworkConnectionLifecycleTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    // -------------------------------------------------------------------------
    // EOF signalling via dispose
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Dispose_WithNoDataWritten_SignalsEofToRemoteReader()
    {
        var (writeEnd, readEnd) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var ct = TestContext.CancellationToken;

        writeEnd.Dispose();

        var buffer = new byte[16];
        var bytesRead = await readEnd.ReadAsync(buffer, ct);

        Assert.AreEqual(0, bytesRead,
            "Disposing the write end must immediately signal EOF to the remote reader.");
    }

    [TestMethod]
    public async Task Dispose_WithPendingData_RemoteReaderDrainsBufferThenReceivesEof()
    {
        var (writeEnd, readEnd) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var ct = TestContext.CancellationToken;

        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        await writeEnd.WriteAsync(ConnectionTestHelpers.Segment(data), ct);
        writeEnd.Dispose();

        var received = await ConnectionTestHelpers.ReadToEndAsync(readEnd, ct);

        CollectionAssert.AreEqual(data, received,
            "All data written before dispose must be readable before EOF.");
    }

    [TestMethod]
    public async Task Dispose_WithMultipleSegments_AllSegmentsDrainedBeforeEof()
    {
        var (writeEnd, readEnd) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var ct = TestContext.CancellationToken;

        await writeEnd.WriteAsync(ConnectionTestHelpers.Segment(0x01, 0x02), ct);
        await writeEnd.WriteAsync(ConnectionTestHelpers.Segment(0x03, 0x04), ct);
        await writeEnd.WriteAsync(ConnectionTestHelpers.Segment(0x05, 0x06), ct);
        writeEnd.Dispose();

        var received = await ConnectionTestHelpers.ReadToEndAsync(readEnd, ct);

        CollectionAssert.AreEqual(
            new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 },
            received);
    }

    [TestMethod]
    public async Task Dispose_UnblocksBlockedRemoteReader_WithEof()
    {
        // If the remote reader is blocked waiting for data, disposing the write
        // end must unblock it with EOF rather than hanging indefinitely.
        var (writeEnd, readEnd) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var ct = TestContext.CancellationToken;

        var buffer = new byte[16];
        var readTask = readEnd.ReadAsync(buffer, ct).AsTask();

        await Task.Yield();
        Assert.IsFalse(readTask.IsCompleted,
            "ReadAsync should be blocked waiting for data before dispose.");

        writeEnd.Dispose();

        var bytesRead = await readTask.WaitAsync(TimeSpan.FromSeconds(5), ct);

        Assert.AreEqual(0, bytesRead,
            "Disposing the write end must unblock the remote reader with EOF.");
    }

    // -------------------------------------------------------------------------
    // Post-dispose behaviour of the disposed connection itself
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task ReadAsync_OnDisposedConnection_ThrowsObjectDisposedException()
    {
        var (_, readEnd) = ConnectionTestHelpers.CreateUnidirectionalPair();

        readEnd.Dispose();

        var buffer = new byte[16];
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(
            async () => await readEnd.ReadAsync(buffer, TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task WriteAsync_OnDisposedConnection_ThrowsObjectDisposedException()
    {
        var (writeEnd, _) = ConnectionTestHelpers.CreateUnidirectionalPair();

        writeEnd.Dispose();

        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(
            async () => await writeEnd.WriteAsync(
                ConnectionTestHelpers.Segment(0x01),
                TestContext.CancellationToken));
    }

    // -------------------------------------------------------------------------
    // Dispose is idempotent
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        var (writeEnd, _) = ConnectionTestHelpers.CreateUnidirectionalPair();

        writeEnd.Dispose();
        writeEnd.Dispose();
        writeEnd.Dispose();

        // No exception — dispose must be idempotent
    }

    [TestMethod]
    public async Task Dispose_CalledTwice_RemoteReaderOnlyReceivesOneEof()
    {
        // A second dispose must not re-release the EOF semaphore and cause
        // phantom data or a spurious second EOF on the reader side.
        var (writeEnd, readEnd) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var ct = TestContext.CancellationToken;

        await writeEnd.WriteAsync(ConnectionTestHelpers.Segment(0xAA), ct);

        writeEnd.Dispose();
        writeEnd.Dispose(); // second dispose must be a strict no-op

        // Read the data byte
        var buffer = new byte[16];
        var firstRead = await readEnd.ReadAsync(buffer, ct);
        Assert.AreEqual(1, firstRead);
        Assert.AreEqual(0xAA, buffer[0]);

        // Read again — must receive exactly EOF, not phantom data
        var eofRead = await readEnd.ReadAsync(buffer, ct);
        Assert.AreEqual(0, eofRead, "Second read must return EOF, not phantom data.");
    }

    // -------------------------------------------------------------------------
    // Direction isolation
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Dispose_OfWriteEnd_DoesNotPreventReadEndFromWritingInReverseDirection()
    {
        // Disposing the write end (ConnectionA) completes only the A→B buffer.
        // The read end (ConnectionB) must still be able to write in its own direction (B→A).
        var (writeEnd, readEnd) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var ct = TestContext.CancellationToken;

        writeEnd.Dispose();

        // ConnectionB should still be able to write in the B→A direction without error
        await readEnd.WriteAsync(ConnectionTestHelpers.Segment(0x42), ct);

        // ConnectionB's read side (A→B) should now see EOF since A disposed its writer
        var buffer = new byte[16];
        var bytesRead = await readEnd.ReadAsync(buffer, ct);
        Assert.AreEqual(0, bytesRead,
            "Read end should see EOF in the A→B direction because the write end was disposed.");
    }

    // -------------------------------------------------------------------------
    // ObservableConnectionStatus integration
    // -------------------------------------------------------------------------

    [TestMethod]
    public void BindStatus_WithNullStatus_ThrowsArgumentNullException()
    {
        var (writeEnd, _) = ConnectionTestHelpers.CreateUnidirectionalPair();

        Assert.ThrowsExactly<ArgumentNullException>(
            () => writeEnd.BindStatus(null!));
    }

    [TestMethod]
    public void BindStatus_CalledTwice_ThrowsInvalidOperationException()
    {
        var (writeEnd, _) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var status = new ObservableConnectionStatus();

        writeEnd.BindStatus(status);

        Assert.ThrowsExactly<InvalidOperationException>(
            () => writeEnd.BindStatus(new ObservableConnectionStatus()));
    }

    [TestMethod]
    public void OnStarted_AfterBindStatus_TransitionsToConnectedState()
    {
        var (writeEnd, _) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var status = new ObservableConnectionStatus();

        writeEnd.BindStatus(status);
        writeEnd.OnStarted();

        Assert.AreEqual(TransportConnectionState.Connected, status.State);
    }

    [TestMethod]
    public void OnStarted_RaisesConnectingThenConnectedEvents()
    {
        var (writeEnd, _) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var status = new ObservableConnectionStatus();
        var events = new List<string>();

        status.Connecting += (_, _) => events.Add("Connecting");
        status.Connected += (_, _) => events.Add("Connected");

        writeEnd.BindStatus(status);
        writeEnd.OnStarted();

        CollectionAssert.AreEqual(
            new[] { "Connecting", "Connected" },
            events);
    }

    [TestMethod]
    public void OnStarted_CalledTwice_IsIdempotent()
    {
        var (writeEnd, _) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var status = new ObservableConnectionStatus();
        var connectedCount = 0;
        status.Connected += (_, _) => connectedCount++;

        writeEnd.BindStatus(status);
        writeEnd.OnStarted();
        writeEnd.OnStarted(); // must be a no-op

        Assert.AreEqual(1, connectedCount,
            "Connected event must fire exactly once even if OnStarted is called twice.");
    }

    [TestMethod]
    public void Dispose_WithBoundAndStartedStatus_TransitionsToDisconnectedState()
    {
        var (writeEnd, _) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var status = new ObservableConnectionStatus();

        writeEnd.BindStatus(status);
        writeEnd.OnStarted();

        Assert.AreEqual(TransportConnectionState.Connected, status.State);

        writeEnd.Dispose();

        Assert.AreEqual(TransportConnectionState.Disconnected, status.State);
    }

    [TestMethod]
    public void Dispose_WithBoundAndStartedStatus_RaisesDisconnectedEvent()
    {
        var (writeEnd, _) = ConnectionTestHelpers.CreateUnidirectionalPair();
        var status = new ObservableConnectionStatus();
        var disconnectedFired = false;
        status.Disconnected += (_, _) => disconnectedFired = true;

        writeEnd.BindStatus(status);
        writeEnd.OnStarted();
        writeEnd.Dispose();

        Assert.IsTrue(disconnectedFired);
    }

    [TestMethod]
    public void Dispose_WithoutBoundStatus_DoesNotThrow()
    {
        // Status is optional — when not bound, dispose must still succeed.
        var (writeEnd, _) = ConnectionTestHelpers.CreateUnidirectionalPair();

        writeEnd.Dispose();

        // No exception expected
    }
}
