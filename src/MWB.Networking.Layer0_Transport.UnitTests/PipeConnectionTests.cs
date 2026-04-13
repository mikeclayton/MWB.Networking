using Microsoft.Extensions.Logging.Abstractions;
using MouseWithoutBorders.Networking.PeerTransport.Layer2_Protocol;
using MWB.Networking.Layer0_Transport.Pipes;
using MWB.Networking.Layer1_Framing;
using System.Diagnostics;
using System.IO.Pipelines;

namespace MWB.Networking.Layer0_Transport.UnitTests;

public class PipeConnectionTests
{
    [TestClass]
    public sealed class SmokeTests
    {
        public TestContext TestContext
        {
            get;
            set;
        }

        [TestMethod]
        public async Task BasicFrameRoundtrip()
        {
            var logger = NullLogger.Instance;

            // simulates a duplex connection
            var serverPipe = new Pipe();
            var clientPipe = new Pipe();

            // open the server connection
            var serverConnection = new PipeNetworkConnection(serverPipe.Reader, clientPipe.Writer);
            var serverAdapter = new NetworkAdapter(
                serverConnection,
                new NetworkFrameWriter(),
                new NetworkFrameReader());

            // open the client connection
            var clientConnection = new PipeNetworkConnection(clientPipe.Reader, serverPipe.Writer);
            var clientAdapter = new NetworkAdapter(
                clientConnection,
                new NetworkFrameWriter(),
                new NetworkFrameReader());

            // write a frame to the server
            var writeFrame = new NetworkFrame(
                kind: NetworkFrameKind.Request,
                eventType: null,
                requestId: 1,
                streamId: null,
                payload: new([0x01, 0x02, 0x03]));
            await clientAdapter.WriteFrameAsync(
                writeFrame,
                TestContext.CancellationToken);

            // read a frame from the server stream
            var readFrame = await serverAdapter.ReadFrameAsync(
                TestContext.CancellationToken);

            // verify the message
            Assert.AreEqual(writeFrame.Kind, readFrame.Kind);
            Assert.AreEqual(writeFrame.RequestId, readFrame.RequestId);
            Assert.AreEqual(writeFrame.StreamId, readFrame.StreamId);
            CollectionAssert.AreEqual(writeFrame.Payload.ToArray(), readFrame.Payload.ToArray());
        }

        [TestMethod]
        public async Task PipePerfTest()
        {
            const int FrameCount = 100_000;

            // create duplex in-memory transport (pipes cross-wired)
            var clientToServer = new Pipe();
            var serverToClient = new Pipe();

            var clientConnection = new PipeNetworkConnection(
                reader: serverToClient.Reader,
                writer: clientToServer.Writer);

            var serverConnection = new PipeNetworkConnection(
                reader: clientToServer.Reader,
                writer: serverToClient.Writer);

            var clientAdapter = new NetworkAdapter(
                clientConnection,
                new NetworkFrameWriter(),
                new NetworkFrameReader());

            var serverAdapter = new NetworkAdapter(
                serverConnection,
                new NetworkFrameWriter(),
                new NetworkFrameReader());

            var payload = new ReadOnlyMemory<byte>([0x01, 0x02, 0x03]);

            var globalStopwatch = new Stopwatch();

            // Writer task
            var writerStopwatch = new Stopwatch();
            var writer = Task.Run(async () =>
            {
                globalStopwatch.Start();
                writerStopwatch.Start();
                for (int i = 0; i < FrameCount; i++)
                {
                    var frame = new NetworkFrame(
                        kind: NetworkFrameKind.Request,
                        eventType: null,
                        requestId: (uint)(i + 1),
                        streamId: null,
                        payload: payload);
                    await clientAdapter.WriteFrameAsync(frame, TestContext.CancellationToken);
                }
                writerStopwatch.Stop();
            }, TestContext.CancellationToken);

            var readerStopwatch = new Stopwatch();
            // Reader task (we time this)
            var reader = Task.Run(async () =>
            {
                readerStopwatch.Start();
                for (int i = 0; i < FrameCount; i++)
                {
                    var frame = await serverAdapter.ReadFrameAsync(TestContext.CancellationToken);
                    // Optional correctness checks (cheap and safe)
                    Assert.AreEqual(NetworkFrameKind.Request, frame.Kind);
                    Assert.AreEqual((uint)(i + 1), frame.RequestId);
                    Assert.AreEqual(payload.Length, frame.Payload.Length);
                }
                readerStopwatch.Stop();
                globalStopwatch.Stop();
            }, TestContext.CancellationToken);

            await Task.WhenAll(writer, reader);

            TestContext.WriteLine(
                $"Read {FrameCount} frames in {globalStopwatch.Elapsed.TotalMilliseconds:F2} ms " +
                $"({FrameCount / globalStopwatch.Elapsed.TotalSeconds:F0} frames/sec)");
        }
    }
}
