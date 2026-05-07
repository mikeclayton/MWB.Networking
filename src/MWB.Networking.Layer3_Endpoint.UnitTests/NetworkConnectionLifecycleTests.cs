using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer0_Transport.Instrumented;
using MWB.Networking.Layer0_Transport.Stack.Lifecycle;
using MWB.Networking.Layer3_Endpoint.Hosting;
using MWB.Networking.Logging;
using MWB.Networking.Logging.Debug;

namespace MWB.Networking.Layer3_Endpoint.UnitTests;

[TestClass]
public sealed class NetworkConnectionLifecycleTests
{
    public TestContext TestContext
    {
        get;
        set;
    }

    //[AssemblyInitialize]
    //public static void AssemblyInit(TestContext context)
    //{
    //    TaskScheduler.UnobservedTaskException += (sender, e) =>
    //    {
    //        // Break into debugger immediately
    //        System.Diagnostics.Debugger.Break();

    //        // Or force test run to fail hard
    //        e.SetObserved();
    //        throw e.Exception;
    //    };
    //}

    [TestCleanup]
    public void Cleanup()
    {
        // force any unobserved exceptions from finalizers to surface during
        // test runs rather than being silently ignored - this makes it easier
        // to determine *which* test caused the issue (and fix it!).
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <summary>
    /// Guards against regression of a real-world bug where protocol sessions
    /// were unexpectedly crashing when the underlying network connection was
    /// replaced by a transport provider.
    ///
    /// This test ensures the Session closes gracefully (rather than crashing)
    /// when a network connection is replaced by the transport provider.
    /// </summary>
    /// <remarks>
    /// Uses a ManualTestConnectionProvider and ManualTestNetworkConnection to
    /// make the test deterministic by explicitly driving connection replacement
    /// and disconnection via test-specific instrumentation methods.
    /// </remarks>
    [TestMethod]
    public async Task ProtocolSession_Stops_Gracefully_When_Network_Connection_Is_Replaced()
    {
        // ------------------------------------------------------------
        // Arrange
        // ------------------------------------------------------------

        //var logger = NullLogger.Instance;
        var (logger, factory) = DebugLoggerFactory.CreateLogger();
        using var loggerScope = logger.BeginMethodLoggingScope(this);
        logger.LogDebug("TEST: If you see this, the logger itself works");

        // Test provider exposing a stable LogicalConnection that can
        // have its underlying physical connections replaced on demand.
        using var manualTestProvider =
            new InstrumentedNetworkConnectionProvider(logger);

        // Two physical network connections to simulate provider churn.
        var connectionA = new InstrumentedNetworkConnection(new ObservableConnectionStatus());
        var connectionB = new InstrumentedNetworkConnection(new ObservableConnectionStatus());

        // Build a protocol session using the real session builder,
        // wiring it to the logical connection rather than a fixed transport.
        var endpoint =
            new SessionEndpointBuilder()
                .UseLogger(logger)
                .UseEvenStreamIds()
                .ConfigurePipelineWith(pipeline =>
                {
                    // Real frame codec pipeline, exactly as in production.
                    pipeline
                        .UseLogger(logger)
                        .UseLengthPrefixedCodec(logger)
                        .UseConnectionProvider(manualTestProvider);
                })
                .Build();

        // ------------------------------------------------------------
        // Act
        // ------------------------------------------------------------

        using var lifecycleCts = new CancellationTokenSource(
            TimeSpan.FromSeconds(5));

        // Attach the first physical connection. From the protocol's
        // perspective, the session now has a usable network transport.
        manualTestProvider.Attach(connectionA);

        // Start the protocol endpoint
        var runTask = endpoint.StartAsync(lifecycleCts.Token);

        // Simulate provider arbitration replacing the active connection
        // with a new physical connection.
        manualTestProvider.Replace(connectionB);

        // The losing connection is disconnected, which causes EOF
        // on reads for that physical transport.
        connectionA.Disconnect();

        // ------------------------------------------------------------
        // Assert
        // ------------------------------------------------------------

        // Current (broken) behavior:
        // The protocol session treats EOF from the replaced connection
        // as terminal and stops processing entirely.
        await runTask;

        Assert.IsTrue(
            runTask.IsCompleted,
            "Protocol session did not terminate promptly after connection replacement.");
    }

    [TestMethod]
    public async Task System_Recovers_When_Session_Terminates_After_Connection_Replacement()
    {
        var logger = NullLogger.Instance;

        using var manualTestProvider =
            new InstrumentedNetworkConnectionProvider(logger);

        var connectionA = new InstrumentedNetworkConnection(new ObservableConnectionStatus());
        var connectionB = new InstrumentedNetworkConnection(new ObservableConnectionStatus());

        int endpointsStarted = 0;

        async Task<SessionEndpoint> RunEndpointAsync(CancellationToken ct)
        {
            Interlocked.Increment(ref endpointsStarted);
            var endpoint =
                new SessionEndpointBuilder()
                    .UseLogger(logger)
                    .UseEvenStreamIds()
                    .ConfigurePipelineWith(pipeline =>
                    {
                        pipeline
                            .UseLogger(logger)
                            .UseLengthPrefixedCodec(logger)
                            .UseConnectionProvider(manualTestProvider);
                    })
                    .Build();
            await endpoint.StartAsync(ct);
            return endpoint;
        }

        using var cts = new CancellationTokenSource(
            TimeSpan.FromSeconds(2));

        // Attach initial physical connection
        manualTestProvider.Attach(connectionA);

        var supervisorTask = Task.Run(async () =>
        {
            // First session (expected to terminate)
            await RunEndpointAsync(cts.Token);

            // Recovery: start a new session
            await RunEndpointAsync(cts.Token);
        }, TestContext.CancellationToken);

        // Replace connection and force termination
        manualTestProvider.Replace(connectionB);
        connectionA.Disconnect();

        await supervisorTask;

        Assert.IsGreaterThanOrEqualTo(
            2, endpointsStarted,
            "System did not recover by starting a new protocol session after termination.");
    }
}
