using KeyboardSharingConsole.CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Hosting;
using MWB.Networking.Layer0_Transport.Tcp;
using MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed;
using System.Diagnostics;
using System.Net;

Console.WriteLine("Hello, World!");

var options = CommandLineParser.Parse(args);
Console.Title =
    $"Producer/Consumer :: listen {options.ListenPort} -> peer {options.ConnectPort}";

// ------------------------------------------------------------
// Logging
// ------------------------------------------------------------
var logger = NullLogger.Instance;
var cts = new CancellationTokenSource();

logger.LogDebug(Console.Title);

// ------------------------------------------------------------
// Determine role
// ------------------------------------------------------------
bool isProducer = options.ListenPort > options.ConnectPort;

Console.WriteLine(isProducer
    ? "[ROLE] Producer"
    : "[ROLE] Consumer");

// ------------------------------------------------------------
// Layer 0: Network connection provider
// ------------------------------------------------------------
Console.WriteLine("creating TCP network connection provider");

var listenEndpoint = isProducer
    ? null
    : new IPEndPoint(IPAddress.Loopback, options.ListenPort);

var connectEndpoint = isProducer
    ? new IPEndPoint(IPAddress.Loopback, options.ConnectPort)
    : null;

var providerConfig = new TcpNetworkConnectionConfig(
    localEndpoint: listenEndpoint,
    remoteEndpoint: connectEndpoint,
    noDelay: true);

using var provider =
    new TcpNetworkConnectionProvider(logger, providerConfig);

Console.WriteLine("opening logical connection");

var handle =
    await provider.OpenConnectionAsync(cts.Token);

// ------------------------------------------------------------
// Layer 2: Protocol session (builder owns wiring)
// ------------------------------------------------------------
Console.WriteLine("creating protocol session");

var sessionBuilder = new ProtocolSessionBuilder()
    .WithLogger(logger)
    .ConfigurePipeline(p =>
        p.AppendFrameCodec(
             new LengthPrefixedFrameEncoder(),
             new LengthPrefixedFrameDecoder())
         .UseConnection(() => handle.Connection));

sessionBuilder =
    isProducer
        ? sessionBuilder.UseEvenStreamIds()
        : sessionBuilder.UseOddStreamIds();

var session = sessionBuilder.Build();

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
// Start session runtime
// ------------------------------------------------------------
Console.WriteLine("starting protocol session");

var runTask =
    session.Lifecycle.StartAsync(cts.Token);

// ------------------------------------------------------------
// Give transport time to settle (PoC only)
// ------------------------------------------------------------
await Task.Delay(500);

// ------------------------------------------------------------
// Send events (Producer only)
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

    Console.WriteLine($"[PRODUCER] queued outbound in {stopwatch.ElapsedMilliseconds} ms");
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