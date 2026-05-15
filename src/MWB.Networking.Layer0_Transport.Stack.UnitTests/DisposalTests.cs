using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer0_Transport.Instrumented;
using MWB.Networking.Layer0_Transport.Stack.Hosting;
using MWB.Networking.Layer0_Transport.Stack.Core.Lifecycle;
using MWB.Networking.Layer0_Transport.Stack.Lifecycle;
using MWB.Networking.Layer0_Transport.Stack.UnitTests.Helpers;

namespace MWB.Networking.Layer0_Transport.Stack.UnitTests;

// ============================================================
//  Disposal tests
// ============================================================

[TestClass]
public sealed class DisposalTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <summary>
    /// DisposeAsync prevents any subsequent ConnectAsync calls.
    /// </summary>
    [TestMethod]
    public async Task DisposeAsync_PreventsSubsequentConnectAsync()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStackBuilder()
            .UseLogger(logger)
            .UseConnectionProvider(provider)
            .OwnsProvider(true)
            .Build();

        await stack.DisposeAsync();

        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(
            () => stack.ConnectAsync());
    }

    /// <summary>
    /// DisposeAsync while connected must gracefully disconnect the active
    /// connection.
    /// </summary>
    [TestMethod]
    public async Task DisposeAsync_WhileConnected_DisconnectsGracefully()
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

        await stack.DisposeAsync();

        // DisconnectAsync is called internally, which should emit Disconnected.
        CollectionAssert.Contains(
            recorder.States.ToList(),
            TransportConnectionState.Disconnected,
            "Disconnected event should fire during disposal.");
    }

    /// <summary>
    /// Multiple DisposeAsync calls must be idempotent.
    /// </summary>
    [TestMethod]
    public async Task DisposeAsync_IsIdempotent()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        var stack = new TransportStackBuilder()
            .UseLogger(logger)
            .UseConnectionProvider(provider)
            .OwnsProvider(true)
            .Build();

        // Should not throw on repeated disposal
        await stack.DisposeAsync();
        await stack.DisposeAsync();
        await stack.DisposeAsync();
    }

    /// <summary>
    /// The synchronous IDisposable.Dispose() must behave consistently with
    /// DisposeAsync (prevents further ConnectAsync calls).
    /// </summary>
    [TestMethod]
    public async Task IDisposable_Dispose_PreventsSubsequentConnectAsync()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStackBuilder()
            .UseLogger(logger)
            .UseConnectionProvider(provider)
            .OwnsProvider(true)
            .Build();

        ((IDisposable)stack).Dispose();

        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(
            () => stack.ConnectAsync());
    }

    /// <summary>
    /// Using the stack in a using block (IDisposable) must not throw even
    /// when the connection is active at scope exit.
    /// </summary>
    [TestMethod]
    public async Task IDisposable_Dispose_WhileConnected_DoesNotThrow()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);

        using (var stack = new TransportStackBuilder()
            .UseLogger(logger)
            .UseConnectionProvider(provider)
            .OwnsProvider(true)
            .Build())
        {
            await stack.ConnectAsync(TestContext.CancellationToken);
            provider.Instrumentation
                .Connection!.Instrumentation
                .OnStarted();
            await stack.AwaitConnectedAsync(TestContext.CancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        }
        // No exception on scope exit.
    }
}
