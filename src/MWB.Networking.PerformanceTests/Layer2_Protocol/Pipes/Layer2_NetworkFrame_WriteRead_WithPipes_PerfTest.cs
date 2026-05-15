using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer0_Transport.Pipes;
using MWB.Networking.Layer1_Framing.Codecs.Default.Network.Hosting;
using MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.Transport.Hosting;
using MWB.Networking.Layer3_Endpoint.Hosting;
using System.IO.Pipelines;

namespace Layer2_Protocol;

public sealed partial class Pipes
{
    /// <summary>
    /// Full end-to-end event delivery test through every layer:
    ///
    ///   client ProtocolSession
    ///     -> SessionAdapter
    ///     -> TransportDriver
    ///     -> NetworkPipeline (DefaultNetworkCodec + LengthPrefixedCodec)
    ///     -> TransportStack
    ///     -> PipeNetworkConnection
    ///     --- in-memory pipe ---
    ///     -> PipeNetworkConnection
    ///     -> TransportStack
    ///     -> NetworkPipeline (DefaultNetworkCodec + LengthPrefixedCodec)
    ///     -> TransportDriver
    ///     -> SessionAdapter
    ///     -> server ProtocolSession
    ///     -> OnEventReceived handler
    /// </summary>
    [TestMethod]
    public async Task Layer2_Protocol_Event_EndToEnd_WithPipes()
    {
        var logger = NullLogger.Instance;

        // Cross-wired in-memory pipes: bytes written by the client are read
        // by the server and vice-versa, simulating a duplex network link.
        var clientToServer = new Pipe();
        var serverToClient = new Pipe();

        // -------------------------------------------------------
        // Build client endpoint (sends events)
        // -------------------------------------------------------
        var clientEndpoint = new SessionEndpointBuilder()
            .UseLogger(logger)
            .UseEvenStreamIds()
            .UseConnectionProvider(
                new PipeNetworkConnectionProvider(
                    logger,
                    reader: serverToClient.Reader,
                    writer: clientToServer.Writer))
            .UsePipeline(pipeline =>
                pipeline
                    .UseLogger(logger)
                    .UseDefaultNetworkCodec()
                    .UseLengthPrefixedCodec(logger))
            .Build();

        // -------------------------------------------------------
        // Build server endpoint (receives events)
        // -------------------------------------------------------
        var received = 0;
        var eventReceived = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var serverEndpoint = new SessionEndpointBuilder()
            .UseLogger(logger)
            .UseOddStreamIds()
            .UseConnectionProvider(
                new PipeNetworkConnectionProvider(
                    logger,
                    reader: clientToServer.Reader,
                    writer: serverToClient.Writer))
            .UsePipeline(pipeline =>
                pipeline
                    .UseLogger(logger)
                    .UseDefaultNetworkCodec()
                    .UseLengthPrefixedCodec(logger))
            .OnEventReceived((_, _) =>
            {
                Interlocked.Increment(ref received);
                eventReceived.TrySetResult();
            })
            .Build();

        // -------------------------------------------------------
        // Start both endpoints
        // -------------------------------------------------------
        using var lifecycleCts = new CancellationTokenSource();

        await clientEndpoint
            .StartAsync(lifecycleCts.Token)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);

        await serverEndpoint
            .StartAsync(lifecycleCts.Token)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);

        // -------------------------------------------------------
        // Act: send a single event from client to server
        // -------------------------------------------------------
        var payload = new ReadOnlyMemory<byte>(new byte[] { 0x01, 0x02, 0x03 });
        clientEndpoint.SendEvent(eventType: 1u, payload: payload);

        // -------------------------------------------------------
        // Assert: event arrives at the server within the timeout
        // -------------------------------------------------------
        await eventReceived.Task
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);

        Assert.AreEqual(1, received,
            "Exactly one event should have been received by the server session.");

        // -------------------------------------------------------
        // Clean shutdown
        // -------------------------------------------------------
        lifecycleCts.Cancel();

        await Task
            .WhenAll(
                clientEndpoint.DisposeAsync().AsTask(),
                serverEndpoint.DisposeAsync().AsTask())
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);
    }
}