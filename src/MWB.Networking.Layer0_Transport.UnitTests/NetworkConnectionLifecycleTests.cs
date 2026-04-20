using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Hosting;
using MWB.Networking.Layer0_Transport.Test;
using MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed;
using MWB.Networking.Logging;
using MWB.Networking.UnitTest.Helpers.Logging;

namespace MWB.Networking.Layer0_Transport.UnitTests;

[TestClass]
public sealed class NetworkConnectionLifecycleTests
{
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
        var (logger, loggerFactory) = DebugLoggerFactory.CreateLogger();
        using var loggerScope = logger.BeginMethodLoggingScope(this);
        logger.LogDebug("TEST: If you see this, the logger itself works");
        logger.LogDebug(nameof(ProtocolSession_Stops_Gracefully_When_Network_Connection_Is_Replaced));

        // Test provider exposing a stable LogicalConnection that can
        // have its underlying physical connections replaced on demand.
        using var manualTestProvider =
            new ManualTestConnectionProvider(logger);

        // Two physical network connections to simulate provider churn.
        var connectionA = new ManualTestNetworkConnection();
        var connectionB = new ManualTestNetworkConnection();

        // Build a protocol session using the real session builder,
        // wiring it to the logical connection rather than a fixed transport.
        var session =
            new ProtocolSessionBuilder()
                .WithLogger(logger)
                .UseEvenStreamIds()
                .ConfigurePipeline(p =>
                {
                    // Real frame codec pipeline, exactly as in production.
                    p.AppendFrameCodec(
                        new LengthPrefixedFrameEncoder(logger),
                        new LengthPrefixedFrameDecoder(logger))
                     // IMPORTANT: bind to the logical connection, not a
                     // specific physical network connection.
                     .UseConnection(() => manualTestProvider.Handle.Connection);
                })
                .Build();

        // ------------------------------------------------------------
        // Act
        // ------------------------------------------------------------

        using var cts = new CancellationTokenSource(
            TimeSpan.FromSeconds(5));

        // Attach the first physical connection. From the protocol's
        // perspective, the session now has a usable network transport.
        manualTestProvider.Attach(connectionA);

        // Start the protocol runtime.
        var runTask = session.StartAsync(cts.Token);

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
            new ManualTestConnectionProvider(logger);

        var connectionA = new ManualTestNetworkConnection();
        var connectionB = new ManualTestNetworkConnection();

        int sessionsStarted = 0;

        async Task RunSessionAsync(CancellationToken ct)
        {
            Interlocked.Increment(ref sessionsStarted);

            var session =
                new ProtocolSessionBuilder()
                    .WithLogger(logger)
                    .UseEvenStreamIds()
                    .ConfigurePipeline(p =>
                    {
                        p.AppendFrameCodec(
                                new LengthPrefixedFrameEncoder(logger),
                                new LengthPrefixedFrameDecoder(logger))
                         .UseConnection(() => manualTestProvider.Handle.Connection);
                    })
                    .Build();

            await session.StartAsync(ct);
        }

        using var cts = new CancellationTokenSource(
            TimeSpan.FromSeconds(2));

        // Attach initial physical connection
        manualTestProvider.Attach(connectionA);

        var supervisorTask = Task.Run(async () =>
        {
            // First session (expected to terminate)
            await RunSessionAsync(cts.Token);

            // Recovery: start a new session
            await RunSessionAsync(cts.Token);
        }, TestContext.CancellationToken);

        // Replace connection and force termination
        manualTestProvider.Replace(connectionB);
        connectionA.Disconnect();

        await supervisorTask;

        Assert.IsTrue(
            sessionsStarted >= 2,
            "System did not recover by starting a new protocol session after termination.");
    }

    public TestContext TestContext { get; set; }
}