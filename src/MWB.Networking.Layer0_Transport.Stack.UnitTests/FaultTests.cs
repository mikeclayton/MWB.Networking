using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer0_Transport.Instrumented;
using MWB.Networking.Layer0_Transport.Stack.Lifecycle;
using MWB.Networking.Layer0_Transport.Stack.UnitTests.Helpers;

namespace MWB.Networking.Layer0_Transport.Stack.UnitTests;

// ============================================================
//  Fault tests
// ============================================================

[TestClass]
public sealed class FaultTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <summary>
    /// When the provider signals a fault on an established connection, the
    /// stack's Faulted event is raised with the matching details.
    /// </summary>
    [TestMethod]
    public async Task SimulateFault_WhileConnected_RaisesStackFaultedEvent()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        TransportFaultedEventArgs? capturedArgs = null;
        stack.Faulted += (_, args) => { capturedArgs = args; };

        await stack.ConnectAsync();
        provider.Instrumentation
            .Connection!.Instrumentation
            .OnStarted();
        await stack.AwaitConnectedAsync()
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        provider.Instrumentation
            .Connection!.Instrumentation
            .SignalFaulted("Injected fault for test.");

        // Faulted event is synchronous; should be set by now.
        Assert.IsNotNull(capturedArgs, "Faulted event must have fired.");
        Assert.AreEqual("Injected fault for test.", capturedArgs!.Message);
    }

    /// <summary>
    /// A provider fault must also emit ConnectionStateChanged(Faulted),
    /// not ConnectionStateChanged(Disconnected).
    /// </summary>
    [TestMethod]
    public async Task SimulateFault_EmitsConnectionStateChanged_Faulted()
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

        provider.Instrumentation
            .Connection!.Instrumentation
            .SignalFaulted("Test-injected fault!");

        await Task.Yield(); // flush any async continuations

        CollectionAssert.Contains(
            recorder.States.ToList(),
            TransportConnectionState.Faulted,
            "ConnectionStateChanged(Faulted) should be raised.");
        CollectionAssert.DoesNotContain(
            recorder.States.ToList(),
            TransportConnectionState.Disconnected,
            "Disconnected state must not be emitted for a fault.");
    }

    /// <summary>
    /// A fault carries the associated exception through the event args.
    /// </summary>
    [TestMethod]
    public async Task SimulateFault_WithException_ExceptionPropagatesInEventArgs()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        TransportFaultedEventArgs? capturedArgs = null;
        stack.Faulted += (_, args) => { capturedArgs = args; };

        var rootCause = new IOException("Simulated network error.");

        await stack.ConnectAsync();
        provider.Instrumentation
            .Connection!.Instrumentation
            .OnStarted();
        await stack.AwaitConnectedAsync()
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        provider.Instrumentation
            .Connection!.Instrumentation
            .SignalFaulted("IO error", rootCause);

        Assert.IsNotNull(capturedArgs);
        Assert.AreSame(rootCause, capturedArgs!.Exception);
    }

    /// <summary>
    /// If OpenConnectionAsync itself throws (e.g., host unreachable), ConnectAsync
    /// must raise the Faulted event before rethrowing.
    /// </summary>
    [TestMethod]
    public async Task ConnectAsync_WhenProviderThrows_RaisesStackFaultedEvent()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        var faultedRaised = false;
        stack.Faulted += (_, _) => { faultedRaised = true; };

        var providerEx = new InvalidOperationException("Host unreachable.");
        provider.Instrumentation
            .SetNextOpenConnectionFailure(providerEx);

        try { await stack.ConnectAsync(); }
        catch (InvalidOperationException) { /* expected */ }

        Assert.IsTrue(faultedRaised,
            "Faulted must fire even when the provider throws during OpenConnectionAsync.");
    }

    /// <summary>
    /// The exception thrown by OpenConnectionAsync is the one surfaced by ConnectAsync.
    /// </summary>
    [TestMethod]
    public async Task ConnectAsync_WhenProviderThrows_PropagatesOriginalException()
    {
        var logger = NullLogger.Instance;
        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

        var providerEx = new InvalidOperationException("Host unreachable.");
        provider.Instrumentation
            .SetNextOpenConnectionFailure(providerEx);

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => stack.ConnectAsync());

        Assert.AreSame(providerEx, ex);
    }
}
