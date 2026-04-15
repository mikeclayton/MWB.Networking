using KeyboardSharingConsole.CommandLine;
using KeyboardSharingConsole.Helpers;
using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport.Tcp;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer3_Runtime;
using System.Net;

Console.WriteLine("Hello, World!");

var options = CommandLineParser.Parse(args);
Console.Title =
    $"Producer/Consumer :: listen {options.ListenPort} -> peer {options.ConnectPort}";


// ------------------------------------------------------------
// Logging
// ------------------------------------------------------------

var logger = LoggingHelper.CreateLogger();
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

session.Observer.EventReceived += (eventType, payload) =>
{
    Console.WriteLine(
        $"[INBOUND] Event {eventType}: {BitConverter.ToString(payload.ToArray())}");
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

    session.Commands.SendEvent(
        eventType: 1,
        payload: new byte[] { (byte)options.ListenPort });
}
else
{
    Console.WriteLine("[CONSUMER] waiting for inbound events");
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