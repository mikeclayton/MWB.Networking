using KeyboardSharingConsole.CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Hosting;
using MWB.Networking.Layer0_Transport.Tcp;
using MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;
using System.Diagnostics;
using System.Net;

Console.WriteLine("Hello, World!");

var options = CommandLineParser.Parse(args);
Console.Title =
    $"Producer/Consumer :: listen {options.ListenPort} -> peer {options.ConnectPort}";

// ------------------------------------------------------------
// Logging & lifetime
// ------------------------------------------------------------
var logger = NullLogger.Instance;
using var cts = new CancellationTokenSource();

logger.LogDebug(Console.Title);

// ------------------------------------------------------------
// Determine role
// ------------------------------------------------------------
var isProducer = options.ListenPort > options.ConnectPort;

Console.WriteLine(isProducer
    ? "[ROLE] Producer"
    : "[ROLE] Consumer");

// ------------------------------------------------------------
// Layer 0: Network connection provider
// ------------------------------------------------------------
Console.WriteLine("creating TCP network connection provider");

using var provider =
    new TcpNetworkConnectionProvider(
        logger,
        new TcpNetworkConnectionConfig(
            localEndpoint: isProducer
                ? null
                : new IPEndPoint(IPAddress.Loopback, options.ListenPort),
            remoteEndpoint: isProducer
                ? new IPEndPoint(IPAddress.Loopback, options.ConnectPort)
                : null,
            noDelay: true)
    );

Console.WriteLine("opening logical connection");

var handle =
    await provider.OpenConnectionAsync(cts.Token);

// ------------------------------------------------------------
// Layer 2: Protocol session (builder owns wiring)
// ------------------------------------------------------------
Console.WriteLine("creating protocol session");

var session =
    new ProtocolSessionBuilder()
        .WithLogger(logger)
        .UseStreamIdParity(
            isProducer
                ? OddEvenStreamIdParity.Even
                : OddEvenStreamIdParity.Odd)
        .ConfigurePipeline(p =>
        {
            p.AppendFrameCodec(
                 new LengthPrefixedFrameEncoder(logger),
                 new LengthPrefixedFrameDecoder(logger))
             .UseConnection(() => handle.Connection);
        })
        .Build();

// ------------------------------------------------------------
// Observe inbound protocol events
// ------------------------------------------------------------
var eventCount = 0;
session.Observer.EventReceived += (eventType, payload) =>
{
    eventCount++;
    Console.WriteLine(
        $"[INBOUND] ({eventCount}) Event {eventType}: {BitConverter.ToString(payload.ToArray())}");
};

// ------------------------------------------------------------
// Start protocol runtime
// ------------------------------------------------------------
Console.WriteLine("starting protocol session");

var runTask =
    session.Lifecycle.StartAsync(cts.Token);

// ------------------------------------------------------------
// Give transport time to settle (PoC only)
// ------------------------------------------------------------
await Task.Delay(500);

// ------------------------------------------------------------
// Send events (producer only)
// ------------------------------------------------------------
if (isProducer)
{
    Console.WriteLine("[PRODUCER] sending hard-coded events");

    await session.Lifecycle.Ready; // forwarded from driver

    var stopwatch = Stopwatch.StartNew();

    for (var i = 0; i < 1000; i++)
    {
        session.Commands.SendEvent(
            eventType: 1,
            payload: BitConverter.GetBytes(options.ListenPort));
    }

    stopwatch.Stop();

    Console.WriteLine(
        $"[PRODUCER] queued outbound in {stopwatch.ElapsedMilliseconds} ms");
}
else
{
    Console.WriteLine("[CONSUMER] waiting for inbound events");
}

// ------------------------------------------------------------
// Shutdown
// ------------------------------------------------------------
Console.WriteLine("Press Enter to exit...");
Console.ReadLine();

cts.Cancel();

try
{
    await runTask;
}
catch (OperationCanceledException)
{
    // Expected on shutdown
}