using KeyboardSharingConsole.CommandLine;
using KeyboardSharingConsole.Helpers;
using MWB.Networking.Layer0_Transport.Tcp;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer3_Runtime;
using System.Net;

Console.WriteLine("Hello, World!");

var options = CommandLineParser.Parse(args);
Console.Title = $"Producer/Consumer :: listen {options.ListenPort} -> peer {options.ConnectPort}";

// ------------------------------------------------------------
// Logging
// ------------------------------------------------------------

var logger = LoggingHelper.CreateLogger();

var cts = new CancellationTokenSource();

// ------------------------------------------------------------
// Determine role for this PoC
// ------------------------------------------------------------
// Producer establishes the outbound connection and sends.
// Consumer listens only and receives.

bool isProducer = options.ListenPort > options.ConnectPort;

Console.WriteLine(isProducer
    ? "[ROLE] Producer"
    : "[ROLE] Consumer");

// ------------------------------------------------------------
// Layer 2: Protocol session
// ------------------------------------------------------------

Console.WriteLine("creating session");

var session = ProtocolSessions.CreateEvenSession(logger);

Console.WriteLine("registering event handler");

session.Observer.EventReceived += (eventType, payload) =>
{
    Console.WriteLine(
        $"[INBOUND] Event {eventType}: {BitConverter.ToString(payload.ToArray())}");
};

// ------------------------------------------------------------
// Layer 0: Listener connection (always)
// ------------------------------------------------------------

Console.WriteLine($"starting listener on {options.ListenPort}");

var listenEndpoint = new IPEndPoint(IPAddress.Loopback, options.ListenPort);

var listenerConnection = new TcpNetworkConnection(
    logger,
    listenEndpoint,
    cts.Token);

// Start reconnect/accept loop in background
_ = listenerConnection.StartAsync();

var listenerAdapter = new NetworkAdapter(
    listenerConnection,
    new NetworkFrameWriter(),
    new NetworkFrameReader());

var listenerDriver =
    new ProtocolDriver(logger, listenerAdapter, session);

// ------------------------------------------------------------
// Layer 0: Outbound connection (Producer only)
// ------------------------------------------------------------

ProtocolDriver? outboundDriver = null;

if (isProducer)
{
    Console.WriteLine($"[PRODUCER] connecting to peer at {options.ConnectPort}");

    var peerEndpoint = new IPEndPoint(IPAddress.Loopback, options.ConnectPort);

    var outboundConnection = new TcpNetworkConnection(
        logger,
        peerEndpoint,
        cts.Token);

    // Start reconnect loop in background
    _ = outboundConnection.StartAsync();

    var outboundAdapter = new NetworkAdapter(
        outboundConnection,
        new NetworkFrameWriter(),
        new NetworkFrameReader());

    outboundDriver =
        new ProtocolDriver(logger, outboundAdapter, session);
}
else
{
    Console.WriteLine("[CONSUMER] no outbound connection (receive only)");
}

// ------------------------------------------------------------
// Layer 3: Start protocol drivers
// ------------------------------------------------------------

Console.WriteLine("running protocol drivers");

var runListener = listenerDriver.RunAsync(cts.Token);
var runOutbound = outboundDriver is not null
    ? outboundDriver.RunAsync(cts.Token)
    : Task.CompletedTask;

// Give listener time to bind and producer time to connect (PoC only)
await Task.Delay(500);

// ------------------------------------------------------------
// Send ONE hard-coded event (Producer only)
// ------------------------------------------------------------

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
    await Task.WhenAll(runListener, runOutbound);
}
catch (OperationCanceledException)
{
    // Expected on shutdown
}
