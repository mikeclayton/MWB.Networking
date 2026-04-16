using KeyboardSharingConsole.CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer0_Transport.Tcp;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer3_Runtime;
using System.Diagnostics;
using System.Net;

Console.WriteLine("Hello, World!");

var options = CommandLineParser.Parse(args);
Console.Title =
    $"Producer/Consumer :: listen {options.ListenPort} -> peer {options.ConnectPort}";


// ------------------------------------------------------------
// Logging
// ------------------------------------------------------------

//var logger = LoggingHelper.CreateLogger();
var logger = NullLogger.Instance;
var cts = new CancellationTokenSource();

logger.LogDebug(Console.Title);

// ------------------------------------------------------------
// Determine role for this PoC
// ------------------------------------------------------------

bool isProducer = options.ListenPort > options.ConnectPort;

Console.WriteLine(isProducer
    ? "[ROLE] Producer"
    : "[ROLE] Consumer");


// ------------------------------------------------------------
// Layer 2: Protocol session
// ------------------------------------------------------------

Console.WriteLine("creating protocol session");

var session = ProtocolSessions.CreateEvenSession(logger);

var eventCount = 0;
session.Observer.EventReceived += (eventType, payload) =>
{
    eventCount++;
    Console.WriteLine(
        $"[INBOUND] ({eventCount}) Event {eventType}: {BitConverter.ToString(payload.ToArray())}");
};


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

// Open logical connection (starts listener/connect loops internally)
Console.WriteLine("opening logical connection");

var handle =
    await provider.OpenConnectionAsync(cts.Token);


// ------------------------------------------------------------
// Layer 1: Framing
// ------------------------------------------------------------

var adapter = new NetworkAdapter(
    handle.Connection,
    new NetworkFrameWriter(),
    new NetworkFrameReader());


// ------------------------------------------------------------
// Layer 3: Protocol driver
// ------------------------------------------------------------

Console.WriteLine("starting protocol driver");

var driver =
    new ProtocolDriver(logger, adapter, session);

var runTask =
    driver.RunAsync(cts.Token);


// ------------------------------------------------------------
// Give transport time to settle (PoC only)
// ------------------------------------------------------------

await Task.Delay(500);


// ------------------------------------------------------------
// Send ONE hard-coded event (Producer only)
// ------------------------------------------------------------

await driver.Ready;

if (isProducer)
{
    Console.WriteLine("[PRODUCER] sending hard-coded event");

    var stopwatch = Stopwatch.StartNew();
    for (var i = 0; i < 1000; i++)
    {
        session.Commands.SendEvent(
            eventType: 1,
            payload: BitConverter.GetBytes(options.ListenPort));
    }
    stopwatch.Stop();

    Console.WriteLine($"[PRODUCER] done queuing outbound in {stopwatch.ElapsedMilliseconds} ms");
    Console.WriteLine($"[PRODUCER] done queuing outbound in {stopwatch.ElapsedTicks} ticks");

    // wait for all events to be transmitted, then read outbound buffer for "enqueued" and "transmitted" timestamps
}
else
{
    Console.WriteLine("[CONSUMER] waiting for inbound events");

    // wait for all events to be received, then read inbound buffer for "received" timestamps

}


// ------------------------------------------------------------
// Keep process alive
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