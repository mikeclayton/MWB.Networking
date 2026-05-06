using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer0_Transport.Instrumented;

namespace MWB.Networking.Layer0_Transport.Stack.UnitTests;

// ============================================================
//  I/O tests
// ============================================================

[TestClass]
public sealed class IoTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <summary>
    /// WriteAsync on an established connection stores the payload for
    /// later inspection via GetWrites().
    /// </summary>
    [TestMethod]
    public async Task WriteAsync_WhenConnected_CapturesPayload()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        await stack.ConnectAsync();
        provider.Instrumentation
            .Connection!.Instrumentation
            .OnStarted();
        await stack.AwaitConnectedAsync()
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        var payload = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        await stack.WriteAsync(new ByteSegments(payload));

        var writes = provider.Instrumentation
            .Connection!.Instrumentation
            .GetWrites();
        Assert.HasCount(1, writes, "Exactly one write should be captured.");

        var segments = writes.First().Segments;
        CollectionAssert.AreEqual(payload, segments[0].ToArray());
    }

    /// <summary>
    /// ReadAsync on a connected stack returns bytes that were injected via
    /// InjectFrame.
    /// </summary>
    [TestMethod]
    public async Task ReadAsync_WhenConnected_ReturnsInjectedData()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        await stack.ConnectAsync();
        provider.Instrumentation
            .Connection!.Instrumentation
            .OnStarted();
        await stack.AwaitConnectedAsync()
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        var injected = new byte[] { 1, 2, 3 };
        provider.Instrumentation
            .Connection!.Instrumentation
            .InjectBytes(injected);

        var buffer = new byte[16];
        var bytesRead = await stack.ReadAsync(buffer)
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        Assert.AreEqual(3, bytesRead);
        CollectionAssert.AreEqual(injected, buffer.Take(bytesRead).ToArray());
    }

    /// <summary>
    /// ReadAsync must gate on the Connected state: it suspends until the
    /// connection is established, then returns injected data.
    /// </summary>
    [TestMethod]
    public async Task ReadAsync_GatesUntilConnected_ThenReadsData()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        await stack.ConnectAsync();
        // Not connected yet — do NOT call OnStarted.

        // Inject data before the gate opens.
        // (It sits in the channel until after connection is established.)
        var injected = new byte[] { 10, 20, 30 };

        var buffer = new byte[16];
        var readTask = stack.ReadAsync(buffer, TestContext.CancellationToken).AsTask();

        // ReadAsync should be suspended waiting for Connected.
        Assert.IsFalse(readTask.IsCompleted,
            "ReadAsync should be gated until the connection is established.");

        // Now inject the frame and open the gate.
        provider.Instrumentation
            .Connection!.Instrumentation
            .InjectBytes(injected);
        provider.Instrumentation
            .Connection!.Instrumentation
            .OnStarted(); // → Connected

        var bytesRead = await readTask
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        Assert.AreEqual(3, bytesRead);
        CollectionAssert.AreEqual(injected, buffer.Take(bytesRead).ToArray());
    }

    /// <summary>
    /// WriteAsync must also gate on the Connected state: it suspends until
    /// the connection is established, then completes.
    /// </summary>
    [TestMethod]
    public async Task WriteAsync_GatesUntilConnected_ThenCaptures()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        await stack.ConnectAsync();
        // Not connected yet.

        var payload = new byte[] { 0xFF };
        var writeTask = stack.WriteAsync(new ByteSegments(payload)).AsTask();

        Assert.IsFalse(writeTask.IsCompleted,
            "WriteAsync should be gated until Connected.");

        provider.Instrumentation
            .Connection!.Instrumentation
            .OnStarted(); // → Connected

        await writeTask
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        Assert.HasCount(1, provider.Instrumentation
            .Connection!.Instrumentation
            .GetWrites());
    }

    /// <summary>
    /// After the provider signals Disconnect, ReadAsync must return 0 (EOF)
    /// rather than blocking indefinitely.
    /// </summary>
    [TestMethod]
    public async Task ReadAsync_AfterProviderDisconnect_ReturnsEof()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        await stack.ConnectAsync();
        provider.Instrumentation
            .Connection!.Instrumentation
            .OnStarted();
        await stack.AwaitConnectedAsync()
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        // Simulate the remote side closing.
        provider.Instrumentation
            .Connection!
            .Disconnect("EOF");

        var buffer = new byte[16];
        var bytesRead = await stack.ReadAsync(buffer)
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        Assert.AreEqual(0, bytesRead, "ReadAsync should return 0 (EOF) after disconnect.");
    }

    /// <summary>
    /// ReadAsync on a stack that has not yet called ConnectAsync throws
    /// InvalidOperationException synchronously.
    /// </summary>
    [TestMethod]
    public void ReadAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        Assert.ThrowsExactly<InvalidOperationException>(
            () => stack.ReadAsync(new byte[16]));
    }

    /// <summary>
    /// WriteAsync on a stack that has not yet called ConnectAsync throws
    /// InvalidOperationException synchronously.
    /// </summary>
    [TestMethod]
    public void WriteAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        Assert.ThrowsExactly<InvalidOperationException>(
            () => stack.WriteAsync(new ByteSegments(new byte[] { 1 })));
    }

    /// <summary>
    /// Cancelling the CancellationToken passed to ReadAsync while it is
    /// gated on the Connected state causes OperationCanceledException.
    /// </summary>
    [TestMethod]
    public async Task ReadAsync_WhenCancelledWhileWaiting_ThrowsOperationCanceledException()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        await stack.ConnectAsync();
        provider.Instrumentation
            .Connection!.Instrumentation
            .SignalConnecting(); // stuck in Connecting

        using var cts = new CancellationTokenSource();
        var readTask = stack.ReadAsync(new byte[16], cts.Token).AsTask();
        Assert.IsFalse(readTask.IsCompleted);

        cts.Cancel();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(
            () => readTask.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    /// <summary>
    /// An exception injected via InjectReadException is propagated through
    /// ReadAsync to the caller.
    /// </summary>
    [TestMethod]
    public async Task ReadAsync_WithInjectedReadException_PropagatesException()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        await stack.ConnectAsync();
        provider.Instrumentation
            .Connection!.Instrumentation
            .OnStarted();
        await stack.AwaitConnectedAsync()
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        var injectedEx = new IOException("Simulated mid-stream network error.");
        provider.Instrumentation
            .Connection!.Instrumentation
            .SetNextReadException(injectedEx);

        var thrown = await Assert.ThrowsExactlyAsync<IOException>(
            () => stack.ReadAsync(new byte[16])
                .AsTask()
                .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken));

        Assert.AreSame(injectedEx, thrown);
    }
}
