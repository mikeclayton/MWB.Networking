
using KeyboardSharingConsole.CommandLine;
using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer0_Transport.Tcp;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Streams;
using MWB.Networking.Layer3_Runtime;
using System.Net;

Console.WriteLine("Hello, World!");

var options = CommandLineParser.Parse(args);
Console.Title = $"Peer @ {options.ListenPort}";

var logger = NullLogger.Instance;
var cts = new CancellationTokenSource();

// ------------------------------------------------------------
// Layer 2: Protocol session
// ------------------------------------------------------------

Console.WriteLine("creating session");

#pragma warning disable CS0618
var session =
    ProtocolSessionFactory.CreateSession(
        new(OddEvenStreamIdParity.Even)); // parity irrelevant for events
#pragma warning restore CS0618

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

// IMPORTANT:
// Start in background – do NOT await
_ = listenerConnection.StartAsync();

var listenerAdapter = new NetworkAdapter(
    listenerConnection,
    new NetworkFrameWriter(),
    new NetworkFrameReader());

var listenerDriver =
    new ProtocolDriver(listenerAdapter, session);

// ------------------------------------------------------------
// Layer 0: Outbound connection (ONLY ONE SIDE CONNECTS)
// ------------------------------------------------------------

ProtocolDriver? outboundDriver = null;

if (options.ListenPort > options.ConnectPort)
{
    Console.WriteLine($"connecting to peer at {options.ConnectPort}");

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
        new ProtocolDriver(outboundAdapter, session);
}
else
{
    Console.WriteLine($"not connecting outbound (peer will connect)");
}

// ------------------------------------------------------------
// Layer 3: Start protocol drivers
// ------------------------------------------------------------

Console.WriteLine("running drivers");

var runListener = listenerDriver.RunAsync(cts.Token);
var runOutbound = outboundDriver is not null
    ? outboundDriver.RunAsync(cts.Token)
    : Task.CompletedTask;

// Give network time to settle
await Task.Delay(500);

// ------------------------------------------------------------
// Send ONE hard‑coded event
// ------------------------------------------------------------

Console.WriteLine("[LOCAL] Sending hard‑coded event");

session.Commands.SendEvent(
    eventType: 1,
    payload: new byte[] { (byte)options.ListenPort });

// ------------------------------------------------------------
// Keep process alive
// ------------------------------------------------------------

Console.WriteLine("Press Enter to exit...");
Console.ReadLine();

cts.Cancel();
await Task.WhenAll(runListener, runOutbound);
