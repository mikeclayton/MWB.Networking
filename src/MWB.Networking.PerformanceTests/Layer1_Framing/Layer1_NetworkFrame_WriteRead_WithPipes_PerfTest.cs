using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer0_Transport.Pipes;
using MWB.Networking.Layer0_Transport.Stack;
using MWB.Networking.Layer0_Transport.Stack.Hosting;
using MWB.Networking.Layer0_Transport.Stack.Lifecycle;
using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer1_Framing.Codecs.Default.Network.Hosting;
using MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.Transport.Hosting;
using MWB.Networking.Layer1_Framing.Pipeline.Hosting;
using MWB.Networking.Logging;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;

namespace Layer1_Framing;

[TestClass]
public sealed partial class Pipes
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
    //        Debugger.Break();

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

    [TestMethod]
    public async Task Layer1_NetworkFrame_WriteRead_WithPipes_PerfTest()
    {
        const int FrameCount = 100_000;

        var logger = NullLogger.Instance;
        //var (logger, loggerFactory) = DebugLoggerFactory.CreateLogger();

        using var loggerScope = logger.BeginMethodLoggingScope(this);

        // create duplex in-memory transport (pipes cross-wired)
        var clientToServer = new Pipe(new PipeOptions(
            pauseWriterThreshold: 1024 * 1024 * 50,
            resumeWriterThreshold: 1024 * 1024 * 50));
        var serverToClient = new Pipe();

        var clientStack = new TransportStackBuilder()
            .UseLogger(logger)
            .UseConnectionProvider(
                new PipeNetworkConnectionProvider(
                    logger, serverToClient.Reader, clientToServer.Writer))
            .Build();

        var serverStack = new TransportStackBuilder()
            .UseLogger(logger)
            .UseConnectionProvider(
                new PipeNetworkConnectionProvider(
                    logger, clientToServer.Reader, serverToClient.Writer))
            .Build();

        // ----------------------------
        // Build client pipeline
        // ----------------------------
        var clientPipeline = new NetworkPipelineBuilder()
            .UseLogger(logger)
            .UseDefaultNetworkCodec()
            .UseLengthPrefixedCodec(logger)
            .Build();

        // ----------------------------
        // Build server pipeline
        // ----------------------------
        var serverPipeline = new NetworkPipelineBuilder()
            .UseLogger(logger)
            .UseDefaultNetworkCodec()
            .UseLengthPrefixedCodec(logger)
            .Build();

        var payload = new ReadOnlyMemory<byte>([0x01, 0x02, 0x03]);

        var globalStopwatch = Stopwatch.StartNew();

        // Writer task
        var writerStopwatch = Stopwatch.StartNew();
        var writer = Task.Run(async () =>
        {
            globalStopwatch.Start();
            writerStopwatch.Start();
            for (int i = 0; i < FrameCount; i++)
            {
                var frame = NetworkFrames.Request(
                    requestId: (uint)(i + 1),
                    payload: payload);
                await clientPipeline.WriteFrameAsync(frame, TestContext.CancellationToken);
            }
            writerStopwatch.Stop();
            await clientToServer.Writer.CompleteAsync();
        }, TestContext.CancellationToken);

        // Reader task
        var readerStopwatch = Stopwatch.StartNew();
        var buffer = new byte[64 * 1024];
        while (true)
        {
            var bytesRead =
                await serverConnection.ReadAsync(buffer, TestContext.CancellationToken);

            if (bytesRead == 0)
            {
                break;
            }

            await serverPipeline
                .DecodeFrameAsync(
                    new ReadOnlySequence<byte>(buffer, 0, bytesRead),
                    TestContext.CancellationToken);
        }
        readerStopwatch.Stop();

        TestContext.WriteLine(
            $"Wrote {FrameCount} frames in {writerStopwatch.Elapsed.TotalMilliseconds:F2} ms " +
            $"({FrameCount / writerStopwatch.Elapsed.TotalSeconds:N0} frames/sec)");

        TestContext.WriteLine(
            $"Read {FrameCount} frames in {readerStopwatch.Elapsed.TotalMilliseconds:F2} ms " +
            $"({FrameCount / readerStopwatch.Elapsed.TotalSeconds:N0} frames/sec)");

        TestContext.WriteLine(
            $"Processed {FrameCount} frames in {globalStopwatch.Elapsed.TotalMilliseconds:F2} ms " +
            $"({FrameCount / globalStopwatch.Elapsed.TotalSeconds:N0} frames/sec)");
    }
}
