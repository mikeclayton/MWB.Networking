using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer0_Transport.Instrumented;
using MWB.Networking.Layer0_Transport.Lifecycle.Exceptions;
using MWB.Networking.Layer0_Transport.Lifecycle.Stack;
using MWB.Networking.Logging.Loggers;

namespace MWB.Networking.Layer0_Transport.Lifecycle.UnitTests.Fuzz;

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
    public async Task Stress_Reconnect_WithSeededFuzz_NoDelays()
    {
        const int BlockCount = 1_024;
        const int BlockSize = 512;
        var stepTimeout = TimeSpan.FromSeconds(5);

        var seed = (int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF);
        TestContext.WriteLine($"Seed = {seed}");

        var rand = new Random(seed);

        //var logger = NullLogger.Instance;
        var (logger, loggerFactory) = DebugLoggerFactory.Create();

        var provider = new InstrumentedNetworkConnectionProvider(logger);
        using var stack = new TransportStack(logger, provider);

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

        // ------------------------------------------------------------
        // Reader (now coordinated + tolerant)
        // ------------------------------------------------------------
        var reader = Task.Run(async () =>
        {
            var buffer = new byte[BlockSize];

            while (received.Count < BlockCount)
            {
                await allowWrite
                    .WaitAsync()
                    .WaitAsync(stepTimeout, TestContext.CancellationToken);

                int read;
                try
                {
                    read = await stack
                        .ReadAsync(buffer)
                        .AsTask()
                        .WaitAsync(stepTimeout, TestContext.CancellationToken);
                }
                catch (TransportDisconnectedException)
                {
                    // Expected during reconnect windows
                    continue;
                }

                if (read == 0)
                    continue;

                var copy = new byte[read];
                Buffer.BlockCopy(buffer, 0, copy, 0, read);
                received.Add(copy);
            }
        });

        // ------------------------------------------------------------
        // Run with global timeout
        // ------------------------------------------------------------
        await Task
            .WhenAll(writer, reader)
            .WaitAsync(TimeSpan.FromSeconds(30), TestContext.CancellationToken);

        // ------------------------------------------------------------
        // Verify integrity
        // ------------------------------------------------------------
        Assert.AreEqual(BlockCount, received.Count, "All blocks must arrive.");

        for (int i = 0; i < BlockCount; i++)
        {
            CollectionAssert.AreEqual(
                expected[i],
                received[i],
                $"Block {i} corrupted or reordered.");
        }
    }

}
