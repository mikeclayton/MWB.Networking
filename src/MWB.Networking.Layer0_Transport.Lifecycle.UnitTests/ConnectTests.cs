using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer0_Transport.Instrumented;

namespace MWB.Networking.Layer0_Transport.Lifecycle.UnitTests;

// ============================================================
//  Connect tests
// ============================================================

[TestClass]
public sealed class ConnectTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <summary>
    /// The happy-path: ConnectAsync returns a ManualNetworkConnection
    /// whose lifecycle the test can then drive manually.
    /// </summary>
    [TestMethod]
    public async Task ConnectAsync_HappyPath_ExposesConnectionForTestControl()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        await stack.ConnectAsync();

        Assert.IsNotNull(provider.Instrumentation.Connection, "Provider should expose the created connection.");
        Assert.HasCount(1, provider.Instrumentation.Connections);
    }

    /// <summary>
    /// A second ConnectAsync call while the first is still in progress must
    /// throw rather than silently create a second concurrent connection.
    /// </summary>
    [TestMethod]
    public async Task ConnectAsync_WhenAlreadyConnecting_ThrowsInvalidOperationException()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        await stack.ConnectAsync(); // drives to Disconnected (initial) — no OnStarted yet

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => stack.ConnectAsync());
    }

    /// <summary>
    /// ConnectAsync must also reject a second call when the connection is
    /// already fully established (Connected state).
    /// </summary>
    [TestMethod]
    public async Task ConnectAsync_WhenAlreadyConnected_ThrowsInvalidOperationException()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        await stack.ConnectAsync();
        provider.Instrumentation
            .Connection!.Instrumentation
            .OnStarted(); // → Connected

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => stack.ConnectAsync());
    }

    /// <summary>
    /// Passing an already-cancelled CancellationToken propagates the
    /// cancellation as OperationCanceledException and leaves the stack
    /// in a clean, reconnectable state.
    /// </summary>
    [TestMethod]
    public async Task ConnectAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => stack.ConnectAsync(cts.Token));
    }

    /// <summary>
    /// After a cancellation, ConnectionStatus must be null so a fresh
    /// ConnectAsync can proceed (stack is reconnectable after CT cancellation).
    /// </summary>
    [TestMethod]
    public async Task ConnectAsync_AfterCancellation_StackIsReconnectable()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // First attempt: cancelled
        try { await stack.ConnectAsync(cts.Token); } catch (OperationCanceledException) { }

        // Second attempt: should succeed without throwing
        await stack.ConnectAsync();
        provider.Instrumentation
            .Connection!.Instrumentation
            .OnStarted();

        await stack.AwaitConnectedAsync()
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        Assert.IsTrue(stack.IsConnected);
    }

    /// <summary>
    /// ConnectAsync on a disposed stack throws ObjectDisposedException.
    /// </summary>
    [TestMethod]
    public async Task ConnectAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);
        await stack.DisposeAsync();

        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(
            () => stack.ConnectAsync());
    }
}
