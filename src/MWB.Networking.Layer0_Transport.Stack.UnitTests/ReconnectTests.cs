using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer0_Transport.Instrumented;
using MWB.Networking.Layer0_Transport.Stack.Hosting;

namespace MWB.Networking.Layer0_Transport.Stack.UnitTests;

// ============================================================
//  Reconnect tests
// ============================================================

[TestClass]
public sealed class ReconnectTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <summary>
    /// The stack must accept a fresh ConnectAsync after a voluntary
    /// DisconnectAsync, using a new underlying connection.
    /// </summary>
    [TestMethod]
    public async Task Reconnect_AfterDisconnect_Succeeds()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStackBuilder()
            .UseLogger(logger)
            .UseConnectionProvider(provider)
            .OwnsProvider(true)
            .Build();

        // First connection
        await stack.ConnectAsync(TestContext.CancellationToken);
        provider.Instrumentation
            .Connection!.Instrumentation
            .OnStarted();
        await stack.AwaitConnectedAsync(TestContext.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        await stack.DisconnectAsync();
        Assert.IsFalse(stack.IsConnected, "Should be disconnected.");

        // Second connection
        await stack.ConnectAsync(TestContext.CancellationToken);
        provider.Instrumentation
            .Connection!.Instrumentation
            .OnStarted();
        await stack.AwaitConnectedAsync(TestContext.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        Assert.IsTrue(stack.IsConnected, "Should be reconnected.");
        Assert.HasCount(2, provider.Instrumentation.Connections,
            "Provider should have created two distinct connections.");
    }

    /// <summary>
    /// The stack must accept a fresh ConnectAsync after a provider-initiated
    /// fault. This is the primary production failure-recovery path.
    /// </summary>
    [TestMethod]
    public async Task Reconnect_AfterFault_Succeeds()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStackBuilder()
            .UseLogger(logger)
            .UseConnectionProvider(provider)
            .OwnsProvider(true)
            .Build();

        // First connection — faulted by provider
        await stack.ConnectAsync(TestContext.CancellationToken);
        provider.Instrumentation
            .Connection!.Instrumentation
            .OnStarted();
        await stack.AwaitConnectedAsync(TestContext.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        provider.Instrumentation
            .Connection!.Instrumentation
            .SignalFaulted("First connection died.");

        await Task.Yield(); // let the Faulted event and cleanup complete

        Assert.IsFalse(stack.IsConnected);

        // Second connection — should succeed
        await stack.ConnectAsync(TestContext.CancellationToken);
        provider.Instrumentation
            .Connection!.Instrumentation
            .OnStarted();
        await stack.AwaitConnectedAsync(TestContext.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        Assert.IsTrue(stack.IsConnected, "Should be connected after fault-recovery.");
        Assert.HasCount(2, provider.Instrumentation.Connections);
    }

    /// <summary>
    /// The stack must be reconnectable after a provider-level open failure.
    /// </summary>
    [TestMethod]
    public async Task Reconnect_AfterProviderOpenFailure_Succeeds()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStackBuilder()
            .UseLogger(logger)
            .UseConnectionProvider(provider)
            .OwnsProvider(true)
            .Build();

        // First attempt: provider throws
        provider.Instrumentation.SetNextOpenConnectionFailure(new Exception("Transient failure."));
        try { await stack.ConnectAsync(TestContext.CancellationToken); } catch { /* expected */ }

        // Second attempt: normal
        await stack.ConnectAsync(TestContext.CancellationToken);
        provider.Instrumentation
            .Connection!.Instrumentation
            .OnStarted();
        await stack.AwaitConnectedAsync(TestContext.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        Assert.IsTrue(stack.IsConnected);
    }
}
