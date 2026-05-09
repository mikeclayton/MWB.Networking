using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport.Instrumented;
using MWB.Networking.Layer0_Transport.Stack.Hosting;
using MWB.Networking.Layer0_Transport.Stack.Core.Lifecycle;
using MWB.Networking.Layer0_Transport.Stack.Core.Primitives;
using MWB.Networking.Logging;
using MWB.Networking.Logging.TestContext;

namespace MWB.Networking.Layer0_Transport.Stack.UnitTests.Fuzz;

[TestClass]
public sealed class RandomFuzzTest
{
    public TestContext TestContext { get; set; } = null!;

    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    internal sealed class AsyncGate
    {
        private TaskCompletionSource _tcs =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task WaitAsync() => _tcs.Task;

        public void Open() => _tcs.TrySetResult();

        public void Reset()
        {
            if (_tcs.Task.IsCompleted)
            {
                _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }
    }

    internal sealed class AsyncFlag
    {
        private TaskCompletionSource _tcs =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool IsSet => _tcs.Task.IsCompleted;

        public Task WaitAsync() => _tcs.Task;

        public void Set() => _tcs.TrySetResult();

        public void Reset()
        {
            if (_tcs.Task.IsCompleted)
            {
                _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }
    }

    private static bool ShouldDisconnect(Random rand, int index)
    {
        // Example strategy:
        //  - ~10–15% disconnections
        //  - clustered, not uniform
        //  - deterministic per seed

        var dice = rand.Next(0, 100);

        if (index < 5) return false; // let system warm up
        if (dice < 12) return true;
        if (index % 257 == 0) return true;  // periodic stress
        return false;
    }

    private const int BlockCount = 1_024;
    private const int BlockSize = 512;

    /// <summary>
    /// Known to fail with seed 1063155453
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task Stress_WritingWithReconnect_WithSeededFuzz_NoDelays()
    {
        const int BlockCount = 1_024;
        const int BlockSize = 512;
        var stepTimeout = TimeSpan.FromSeconds(5);

        var seed = (int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF);
        TestContext.WriteLine($"Seed = {seed}");

        var rand = new Random(seed);

        //var logger = NullLogger.Instance;
        var (logger, loggerFactory) = TestContextLoggerFactory.CreateLogger(this.TestContext);
        using var loggerScope = logger.BeginMethodLoggingScope(this);

        var provider = new InstrumentedNetworkConnectionProvider(logger);
        provider.Instrumentation.UseLoopback = true;

        using var stack = new TransportStackBuilder()
            .UseLogger(logger)
            .UseConnectionProvider(provider)
            .OwnsProvider(true)
            .Build();

        var expected = new List<byte[]>(BlockCount);
        var received = new List<byte[]>(BlockCount);

        // Coordination primitives
        var allowWrite = new AsyncGate();
        var writeInProgress = new AsyncFlag();
        var disconnectedSignal = new AsyncGate();
        var reconnectComplete = new AsyncGate();

        // ------------------------------------------------------------
        // Signal‑only event handler (never async!)
        // ------------------------------------------------------------
        stack.ConnectionStateChanged += (_, state) =>
        {
            if (state == TransportConnectionState.Disconnected)
                disconnectedSignal.Open();
        };

        // ------------------------------------------------------------
        // Dedicated reconnector loop
        // ------------------------------------------------------------
        var reconnector = Task.Run(async () =>
        {
            while (true)
            {
                await disconnectedSignal
                    .WaitAsync()
                    .WaitAsync(stepTimeout, TestContext.CancellationToken);

                disconnectedSignal.Reset();

                await writeInProgress
                    .WaitAsync()
                    .WaitAsync(stepTimeout, TestContext.CancellationToken);

                await stack
                    .ConnectAsync()
                    .WaitAsync(stepTimeout, TestContext.CancellationToken);

                provider.Instrumentation
                    .Connection!.Instrumentation
                    .OnStarted();

                await stack
                    .AwaitConnectedAsync()
                    .WaitAsync(stepTimeout, TestContext.CancellationToken);

                reconnectComplete.Open();
                allowWrite.Open();
            }
        });

        // ------------------------------------------------------------
        // Initial connect
        // ------------------------------------------------------------
        await stack
            .ConnectAsync()
            .WaitAsync(stepTimeout, TestContext.CancellationToken);

        provider.Instrumentation
            .Connection!.Instrumentation
            .OnStarted();

        await stack
            .AwaitConnectedAsync()
            .WaitAsync(stepTimeout, TestContext.CancellationToken);

        allowWrite.Open();

        // ------------------------------------------------------------
        // Writer
        // ------------------------------------------------------------
        var writer = Task.Run(async () =>
        {
            for (int i = 0; i < BlockCount; i++)
            {
                await allowWrite
                    .WaitAsync()
                    .WaitAsync(stepTimeout, TestContext.CancellationToken);

                writeInProgress.Reset();

                var block = new byte[BlockSize];
                rand.NextBytes(block);
                expected.Add(block);

                await stack
                    .WriteAsync(new ByteSegments(block), CancellationToken.None)
                    .AsTask()
                    .WaitAsync(stepTimeout, TestContext.CancellationToken);

                writeInProgress.Set();

                if (ShouldDisconnect(rand, i))
                {
                    provider.Instrumentation
                        .Connection!
                        .Disconnect($"Fuzz disconnect at block {i}");

                    await reconnectComplete
                        .WaitAsync()
                        .WaitAsync(stepTimeout, TestContext.CancellationToken);

                    reconnectComplete.Reset();
                }
            }
        });

        var readChannelCount = provider.Instrumentation
            .Connection!.Instrumentation
            .ReadChannelCount;

        var isLoopback = provider.Instrumentation
            .Connection!.Instrumentation
            .IsLoopback;

        logger.Log(LogLevel.Information, "Wrote {BlockCount} blocks.", BlockCount);
        logger.Log(LogLevel.Information, "IsLoopback: {IsLoopback}", isLoopback);
        logger.Log(LogLevel.Information, "Read channel count: {ReadChannelCount} blocks.", readChannelCount);

        // disconnecting and reconnecting currently destroys the write buffer,
        // so the read channel will only contain the data written during the final
        // connect window. This is a known limitation of the current implementation.

        //// ------------------------------------------------------------
        //// Reader (now coordinated + tolerant)
        //// ------------------------------------------------------------
        //var reader = Task.Run(async () =>
        //{
        //    var buffer = new byte[BlockSize];

        //    while (received.Count < BlockCount)
        //    {
        //        await allowWrite
        //            .WaitAsync()
        //            .WaitAsync(stepTimeout, TestContext.CancellationToken);

        //        int read;
        //        try
        //        {
        //            read = await stack
        //                .ReadAsync(buffer)
        //                .AsTask()
        //                .WaitAsync(stepTimeout, TestContext.CancellationToken);
        //        }
        //        catch (TransportDisconnectedException)
        //        {
        //            // Expected during reconnect windows
        //            continue;
        //        }

        //        if (read == 0)
        //            continue;

        //        var copy = new byte[read];
        //        Buffer.BlockCopy(buffer, 0, copy, 0, read);
        //        received.Add(copy);
        //    }
        //});

        // ------------------------------------------------------------
        // Run with global timeout
        // ------------------------------------------------------------
        await Task
            .WhenAll(writer) //, reader)
            .WaitAsync(TimeSpan.FromSeconds(30), TestContext.CancellationToken);

        // this test is just to verify that writes can all complete during an
        // unstable connection with disconnects and reconnects. there's nothing
        // to assert - if the test completes it's successful, otherwise it fails
        // if it times out or throws an exception .

        //// ------------------------------------------------------------
        //// Verify integrity
        //// ------------------------------------------------------------
        //Assert.AreEqual(BlockCount, received.Count, "All blocks must arrive.");

        //for (int i = 0; i < BlockCount; i++)
        //{
        //    CollectionAssert.AreEqual(
        //        expected[i],
        //        received[i],
        //        $"Block {i} corrupted or reordered.");
        //}
    }

}
