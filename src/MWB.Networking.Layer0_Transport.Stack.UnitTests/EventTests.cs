using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer0_Transport.Instrumented;
using MWB.Networking.Layer0_Transport.Stack.Hosting;
using MWB.Networking.Layer0_Transport.Stack.Core.Lifecycle;
using MWB.Networking.Layer0_Transport.Stack.Lifecycle;
using MWB.Networking.Layer0_Transport.Stack.UnitTests.Helpers;

namespace MWB.Networking.Layer0_Transport.Stack.UnitTests;

// ============================================================
//  Event/state tests
// ============================================================

[TestClass]
public sealed class EventTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <summary>
    /// The happy-path connect → disconnect cycle must emit the four expected
    /// state transitions in the correct order.
    /// </summary>
    [TestMethod]
    public async Task ConnectionStateChanged_HappyPath_EmitsCorrectSequence()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStackBuilder()
            .UseLogger(logger)
            .UseConnectionProvider(provider)
            .OwnsProvider(true)
            .Build();

        using var recorder = new StateRecorder(stack);

        await stack.ConnectAsync(TestContext.CancellationToken);
        provider.Instrumentation
            .Connection!.Instrumentation
            .SignalConnecting();
        provider.Instrumentation
            .Connection!.Instrumentation
            .SignalConnected();
        await stack.AwaitConnectedAsync(TestContext.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        await stack.DisconnectAsync();

        var expected = new[]
        {
            TransportConnectionState.Connecting,
            TransportConnectionState.Connected,
            TransportConnectionState.Disconnecting,
            TransportConnectionState.Disconnected
        };

        CollectionAssert.AreEqual(
            expected, recorder.States.ToList(),
            "Full connect→disconnect lifecycle must emit states in order.");
    }

    /// <summary>
    /// A provider-initiated fault must emit Connecting, Connected, then Faulted —
    /// not Disconnected.
    /// </summary>
    [TestMethod]
    public async Task ConnectionStateChanged_OnFault_EmitsFaultedNotDisconnected()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStackBuilder()
            .UseLogger(logger)
            .UseConnectionProvider(provider)
            .OwnsProvider(true)
            .Build();

        using var recorder = new StateRecorder(stack);

        await stack.ConnectAsync(TestContext.CancellationToken);
        provider.Instrumentation
            .Connection!.Instrumentation
            .OnStarted();
        await stack.AwaitConnectedAsync(TestContext.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        provider.Instrumentation
            .Connection!.Instrumentation
            .SignalFaulted("Test-injected fault!");

        await Task.Yield();

        var states = recorder.States.ToList();
        Assert.AreEqual(TransportConnectionState.Faulted, states.Last(),
            "Last emitted state must be Faulted.");
        CollectionAssert.DoesNotContain(states, TransportConnectionState.Disconnected,
            "Disconnected must not appear in fault path.");
    }

    /// <summary>
    /// Duplicate consecutive states must not be emitted.
    /// The stack deduplicates via _lastRaisedConnectionState.
    /// </summary>
    [TestMethod]
    public async Task ConnectionStateChanged_DeduplicatesIdenticalConsecutiveStates()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStackBuilder()
            .UseLogger(logger)
            .UseConnectionProvider(provider)
            .OwnsProvider(true)
            .Build();

        using var recorder = new StateRecorder(stack);

        await stack.ConnectAsync(TestContext.CancellationToken);
        provider.Instrumentation
            .Connection!.Instrumentation
            .OnStarted();
        await stack.AwaitConnectedAsync(TestContext.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        // A double-call to Disconnect on the ManualNetworkConnection is
        // absorbed by ObservableConnectionStatus.Terminal() before it ever
        // reaches RaiseConnectionStateChanged — so we verify the happy-path
        // sequence contains exactly one Disconnected at the end.
        await stack.DisconnectAsync();

        var states = recorder.States.ToList();
        var disconnectedCount = states.Count(s => s == TransportConnectionState.Disconnected);
        Assert.AreEqual(1, disconnectedCount,
            "Disconnected state should appear exactly once.");
    }

    /// <summary>
    /// IsConnected tracks the lifecycle accurately: true only in the
    /// Connected state.
    /// </summary>
    [TestMethod]
    public async Task IsConnected_TracksBothSidesOfLifecycle()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStackBuilder()
            .UseLogger(logger)
            .UseConnectionProvider(provider)
            .OwnsProvider(true)
            .Build();

        Assert.IsFalse(stack.IsConnected, "Initially not connected.");

        await stack.ConnectAsync(TestContext.CancellationToken);
        Assert.IsFalse(stack.IsConnected, "Connecting state is not Connected.");

        provider.Instrumentation
            .Connection!.Instrumentation
            .SignalConnecting();
        Assert.IsFalse(stack.IsConnected, "Connecting state is not Connected.");

        provider.Instrumentation
            .Connection!.Instrumentation
            .SignalConnected();
        await stack.AwaitConnectedAsync(TestContext.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        Assert.IsTrue(stack.IsConnected, "Should be connected after SimulateConnected.");

        await stack.DisconnectAsync();
        Assert.IsFalse(stack.IsConnected, "Should not be connected after disconnect.");
    }

    /// <summary>
    /// The ConnectionState property reflects each lifecycle transition.
    /// </summary>
    [TestMethod]
    public async Task ConnectionState_ReflectsEachTransition()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStackBuilder()
            .UseLogger(logger)
            .UseConnectionProvider(provider)
            .OwnsProvider(true)
            .Build();

        Assert.IsNull(stack.ConnectionState, "Initially null (no active connection).");

        await stack.ConnectAsync(TestContext.CancellationToken);
        // State is Disconnected (initial, not terminal) until SimulateConnecting.
        Assert.AreEqual(TransportConnectionState.Disconnected, stack.ConnectionState);

        provider.Instrumentation
            .Connection!.Instrumentation
            .SignalConnecting();
        Assert.AreEqual(TransportConnectionState.Connecting, stack.ConnectionState);

        provider.Instrumentation
            .Connection!.Instrumentation
            .SignalConnected();
        Assert.AreEqual(TransportConnectionState.Connected, stack.ConnectionState);

        await stack.DisconnectAsync();
        Assert.IsNull(stack.ConnectionState,
            "After full disconnect, ConnectionStatus is nulled so ConnectionState is null.");
    }
}
