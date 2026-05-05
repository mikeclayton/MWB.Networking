using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer0_Transport.Instrumented;
using MWB.Networking.Layer0_Transport.Lifecycle.Stack;
using MWB.Networking.Layer0_Transport.Lifecycle.UnitTests.Helpers;

namespace MWB.Networking.Layer0_Transport.Lifecycle.UnitTests;

// ============================================================
//  Disconnect tests
// ============================================================

[TestClass]
public sealed class DisconnectTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <summary>
    /// DisconnectAsync on a fully established connection must emit
    /// Disconnecting then Disconnected state transitions.
    /// </summary>
    [TestMethod]
    public async Task DisconnectAsync_EmitsDisconnectingThenDisconnected()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);
        using var recorder = new StateRecorder(stack);

        await stack.ConnectAsync();
        provider.Instrumentation
            .Connection!.Instrumentation
            .OnStarted();
        await stack.AwaitConnectedAsync()
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        await stack.DisconnectAsync();

        var states = recorder.States;
        CollectionAssert.Contains(
            states.ToList(), TransportConnectionState.Disconnecting,
            "Disconnecting should be emitted.");
        CollectionAssert.Contains(
            states.ToList(), TransportConnectionState.Disconnected,
            "Disconnected should be emitted.");

        // Disconnecting must precede Disconnected
        var idxDisconnecting = states.ToList().IndexOf(TransportConnectionState.Disconnecting);
        var idxDisconnected = states.ToList().IndexOf(TransportConnectionState.Disconnected);
        Assert.IsLessThan(idxDisconnected, idxDisconnecting,
            "Disconnecting must be emitted before Disconnected.");
    }

    /// <summary>
    /// DisconnectAsync when not connected must be a safe no-op.
    /// </summary>
    [TestMethod]
    public async Task DisconnectAsync_WhenNotConnected_IsNoOp()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        // Should not throw
        await stack.DisconnectAsync();
        Assert.IsFalse(stack.IsConnected);
    }

    /// <summary>
    /// After DisconnectAsync the stack is no longer connected and
    /// IsConnected reflects that immediately.
    /// </summary>
    [TestMethod]
    public async Task DisconnectAsync_ClearsConnectedState()
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

        Assert.IsTrue(stack.IsConnected, "Pre-condition: should be connected.");

        await stack.DisconnectAsync();

        Assert.IsFalse(stack.IsConnected);
    }

    /// <summary>
    /// A provider-initiated disconnect (ManualNetworkConnection.Disconnect)
    /// should raise the Disconnected state on the stack, not Faulted.
    /// </summary>
    [TestMethod]
    public async Task Disconnect_FromProvider_EmitsDisconnectedNotFaulted()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);
        using var recorder = new StateRecorder(stack);

        var faultedRaised = false;
        stack.Faulted += (_, _) => { faultedRaised = true; };

        await stack.ConnectAsync();
        provider.Instrumentation
            .Connection!.Instrumentation
            .OnStarted();
        await stack.AwaitConnectedAsync()
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        // Simulate the remote side closing the connection.
        provider.Instrumentation
            .Connection!.Disconnect("Remote closed.");

        // Give event handlers a moment to fire.
        await Task.Yield();

        Assert.IsFalse(faultedRaised, "Faulted event must not fire on graceful disconnect.");
        CollectionAssert.Contains(
            recorder.States.ToList(),
            TransportConnectionState.Disconnected,
            "ConnectionStateChanged(Disconnected) should have been raised.");
        Assert.IsFalse(stack.IsConnected);
    }
}
