using MWB.Networking.Layer0_Transport.Pipes;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer2_Protocol.Requests;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Streams;
using System.IO.Pipelines;

namespace MWB.Networking.Layer3_Runtime.UnitTests;

[TestClass]
public sealed class ProtocolDriverEndToEndTests
{
    public TestContext TestContext
    {
        get;
        set;
    }

    [TestMethod]
    public async Task ProtocolDriver_Transmits_ProtocolFrames_EndToEnd()
    {
        // ------------------------------------------------------------
        // Arrange: connected in-memory duplex network
        // ------------------------------------------------------------
        // Two pipes simulate a bidirectional network connection
        var serverPipe = new Pipe();
        var clientPipe = new Pipe();

        // Server connection: reads what client writes, writes back to client
        var serverConnection =
            new PipeNetworkConnection(serverPipe.Reader, clientPipe.Writer);
        var serverAdapter = new NetworkAdapter(
            serverConnection,
            new NetworkFrameWriter(),
            new NetworkFrameReader());

        // Client connection: reads what server writes, writes to server
        var clientConnection =
            new PipeNetworkConnection(clientPipe.Reader, serverPipe.Writer);
        var clientAdapter = new NetworkAdapter(
            clientConnection,
            new NetworkFrameWriter(),
            new NetworkFrameReader());

        // Protocol sessions (Layer 2)
#pragma warning disable CS0618 // Type or member is obsolete
        var serverSession = ProtocolSessionFactory.CreateSession(new(OddEvenStreamIdParity.Odd));
        var clientSession = ProtocolSessionFactory.CreateSession(new(OddEvenStreamIdParity.Even));
#pragma warning restore CS0618 // Type or member is obsolete

        // Protocol drivers (Layer 3)
        var serverDriver = new ProtocolDriver(serverAdapter, serverSession);
        var clientDriver = new ProtocolDriver(clientAdapter, clientSession);

        // ------------------------------------------------------------
        // Start both drivers
        // ------------------------------------------------------------
        using var cts = new CancellationTokenSource();
        var runServer = serverDriver.RunAsync(cts.Token);
        var runClient = clientDriver.RunAsync(cts.Token);

        await Task.Yield();

        // ------------------------------------------------------------
        // Capture inbound delivery on the server side
        // ------------------------------------------------------------
        // IMPORTANT:
        // Assert delivery via inbound callbacks, not AwaitOutboundAsync.
        var eventTcs =
            new TaskCompletionSource<(uint EventType, ReadOnlyMemory<byte> Payload)>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        var requestTcs =
            new TaskCompletionSource<IncomingRequest>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        serverSession.Observer.EventReceived += (evt, payload) =>
        {
            eventTcs.TrySetResult((evt, payload));
        };

        serverSession.Observer.RequestReceived += (req, payload) =>
        {
            requestTcs.TrySetResult(req);
        };

        // ------------------------------------------------------------
        // Act: application sends protocol data via the session API
        // ------------------------------------------------------------

        // IMPORTANT:
        // Applications use SendEvent / SendRequest.
        // They do NOT inject frames or touch adapters.

        var eventPayload = new byte[] { 0xDE, 0xAD };
        var requestPayload = new byte[] { 0xBE, 0xEF };

        // Client application sends an event
        clientSession.Commands.SendEvent(1, eventPayload);

        // Client application sends a request
        _ = clientSession.Commands.SendRequest(requestPayload);

        // Give the drivers time to move bytes through the pipes
        var completedEvent = await Task.WhenAny(
            eventTcs.Task,
            Task.Delay(TimeSpan.FromSeconds(120), TestContext.CancellationToken));
        Assert.AreSame(
            eventTcs.Task, completedEvent,
            "Timed out waiting for EventReceived");
        var receivedEvent = await eventTcs.Task;

        var completedRequest = await Task.WhenAny(
            requestTcs.Task,
            Task.Delay(TimeSpan.FromSeconds(120), TestContext.CancellationToken));
        Assert.AreSame(
            requestTcs.Task, completedRequest,
            "Timed out waiting for EventReceived");
        var receivedRequest = await requestTcs.Task;

        // ------------------------------------------------------------
        // Assert: server session received inbound frames
        // ------------------------------------------------------------

        // Event should arrive via EventReceived
        Assert.AreEqual(1u, receivedEvent.EventType);
        CollectionAssert.AreEqual(
            eventPayload,
            receivedEvent.Payload.ToArray());

        // Request should arrive via RequestReceived
        Assert.IsNotNull(receivedRequest);
        Assert.AreEqual(1u, receivedRequest.Context.RequestId);

        // ------------------------------------------------------------
        // Cleanup
        // ------------------------------------------------------------
        cts.Cancel();
        await Task.WhenAll(runServer, runClient);
    }
}
