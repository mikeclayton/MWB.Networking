using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer0_Transport.Instrumented;
using MWB.Networking.Layer0_Transport.Lifecycle.Exceptions;

namespace MWB.Networking.Layer0_Transport.Lifecycle.UnitTests;

// ============================================================
//  AwaitConnected tests
// ============================================================

[TestClass]
public sealed class AwaitConnectedTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <summary>
    /// AwaitConnectedAsync takes the fast-path and returns a completed task
    /// when the connection is already in the Connected state.
    /// </summary>
    [TestMethod]
    public async Task AwaitConnectedAsync_WhenAlreadyConnected_ReturnsImmediately()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        await stack.ConnectAsync();
        provider.Instrumentation
            .Connection!.Instrumentation
            .OnStarted(); // → Connected

        var task = stack.AwaitConnectedAsync();
        Assert.IsTrue(task.IsCompleted, "Should complete synchronously via fast-path.");
        await task; // no exception
    }

    /// <summary>
    /// AwaitConnectedAsync suspends until SimulateConnected drives the status
    /// to Connected, then completes normally.
    /// </summary>
    [TestMethod]
    public async Task AwaitConnectedAsync_WaitsUntilConnected()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        await stack.ConnectAsync();
        // Connection is in initial Disconnected state; not yet Connected.

        var awaitTask = stack.AwaitConnectedAsync();
        Assert.IsFalse(awaitTask.IsCompleted, "Should be suspended before Connected fires.");

        // Drive the connection forward on a background thread to avoid deadlock.
        _ = Task.Run(() =>
        {
            provider.Instrumentation
                .Connection!.Instrumentation
                .OnStarted();
        });

        await awaitTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
    }

    /// <summary>
    /// If the provider signals a fault while AwaitConnectedAsync is waiting,
    /// the returned task must complete with TransportFaultException.
    /// </summary>
    [TestMethod]
    public async Task AwaitConnectedAsync_WhenFaultedWhileWaiting_ThrowsTransportFaultException()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        await stack.ConnectAsync();
        var awaitTask = stack.AwaitConnectedAsync();
        Assert.IsFalse(awaitTask.IsCompleted);

        provider.Instrumentation
            .Connection!.Instrumentation
            .SignalFaulted("Test-injected fault.");

        var ex = await Assert.ThrowsExactlyAsync<TransportFaultException>(
            () => awaitTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken));

        Assert.IsNotNull(ex);
    }

    /// <summary>
    /// If the provider signals a graceful disconnect while AwaitConnectedAsync
    /// is waiting, the returned task must complete with
    /// TransportDisconnectedException.
    /// </summary>
    [TestMethod]
    public async Task AwaitConnectedAsync_WhenDisconnectedWhileWaiting_ThrowsTransportDisconnectedException()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        await stack.ConnectAsync();
        var awaitTask = stack.AwaitConnectedAsync();
        Assert.IsFalse(awaitTask.IsCompleted);

        provider.Instrumentation
            .Connection!.Disconnect("Remote side closed.");

        var ex = await Assert.ThrowsExactlyAsync<TransportDisconnectedException>(
            () => awaitTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken));

        Assert.IsNotNull(ex);
    }

    /// <summary>
    /// AwaitConnectedAsync must throw synchronously when called before any
    /// ConnectAsync has been issued.
    /// </summary>
    [TestMethod]
    public void AwaitConnectedAsync_WhenNotConnecting_ThrowsInvalidOperationException()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        Assert.Throws<InvalidOperationException>(
            () => stack.AwaitConnectedAsync());
    }

    /// <summary>
    /// AwaitConnectedAsync must also throw after a completed disconnect (the
    /// stack is no longer in a connecting or connected state).
    /// </summary>
    [TestMethod]
    public async Task AwaitConnectedAsync_AfterDisconnect_ThrowsInvalidOperationException()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        await stack.ConnectAsync();
        provider.Instrumentation
            .Connection!.Instrumentation
            .OnStarted();
        await stack.DisconnectAsync();

        Assert.ThrowsExactly<InvalidOperationException>(
            () => stack.AwaitConnectedAsync());
    }
}
