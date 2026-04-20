using KeyboardSharingConsole.CommandLine;
using KeyboardSharingConsole.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer0_Transport.Tcp;

Console.WriteLine("Hello, World!");

var options = CommandLineParser.Parse(args);
Console.Title =
    $"peer {options.LocalPeerName} listen {options.ListenPort} -> peer {options.ConnectPort}";

// ------------------------------------------------------------
// Logging & lifetime
// ------------------------------------------------------------
var logger = NullLogger.Instance;
//var logger = LoggingHelper.CreateLogger();
using var cts = new CancellationTokenSource();

logger.LogDebug(Console.Title);

// ------------------------------------------------------------
// Determine role
// ------------------------------------------------------------

logger.LogDebug("[LOCAL PEER NAME] {LocalPeerName}", options.LocalPeerName);
logger.LogDebug("[REMOTE PEER NAME] {RemotePeerName}", options.RemotePeerName);

// ------------------------------------------------------------
// Layer 0: Network connection provider
// ------------------------------------------------------------
logger.LogDebug("creating network connection provider");

var isPeerA = (options.LocalPeerName == "PeerA");
var preferredArbitrationDirection = isPeerA
    ? ConnectionDirection.Outbound
    : ConnectionDirection.Inbound;

Console.WriteLine(
    $"[ARBITRATION] Preferred: {preferredArbitrationDirection}");

//// temporary until we have an automatic, zero-config arbitrator
//using var provider =
//    new TcpNetworkConnectionProvider(
//        logger,
//        new TcpNetworkConnectionConfig(
//            localEndpoint: new IPEndPoint(
//                IPAddress.Loopback, options.ListenPort),
//            remoteEndpoint: new IPEndPoint(
//                IPAddress.Loopback, options.ConnectPort),
//            noDelay: true),
//        preferredArbitrationDirection: preferredArbitrationDirection
//    );

using var provider = await NamedPipeHelper.CreateNamedPipeConnectionProviderAsync(
    logger, options.LocalPeerName, options.RemotePeerName, cts.Token);

Console.WriteLine("opening logical connection");
var connectionHandle =
    await provider.OpenConnectionAsync(cts.Token);

// ------------------------------------------------------------
// Layer 2: Protocol session (builder owns wiring)
// ------------------------------------------------------------
Console.WriteLine("creating protocol session");
var keyboardEventConsumer = new KeyboardEventConsumer();
var session = ProtocolSessionHelper.CreateSession(
    logger, isPeerA, connectionHandle.Connection, keyboardEventConsumer.OnEventReceived);

// ------------------------------------------------------------
// Start protocol runtime
// ------------------------------------------------------------
Console.WriteLine("starting protocol session");
var runTask = session.StartAsync(cts.Token);

// ------------------------------------------------------------
// Send events
// ------------------------------------------------------------
await KeyboardProducerLoop.RunAsync(
    session,
    cts.Token,
    eventType: 1);

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