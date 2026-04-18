using KeyboardSharingConsole.CommandLine;
using KeyboardSharingConsole.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer0_Transport.Tcp;
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
var connectionHandle =
    await provider.OpenConnectionAsync(cts.Token);

// ------------------------------------------------------------
// Layer 2: Protocol session (builder owns wiring)
// ------------------------------------------------------------
Console.WriteLine("creating protocol session");
var keyboardEventConsumer = new KeyboardEventConsumer();
var session = ProtocolSessionHelper.CreateSession(
    logger, isProducer, connectionHandle.Connection, keyboardEventConsumer.OnEventReceived);

// ------------------------------------------------------------
// Start protocol runtime
// ------------------------------------------------------------
Console.WriteLine("starting protocol session");
var runTask = session.StartAsync(cts.Token);

// ------------------------------------------------------------
// Send events (producer only)
// ------------------------------------------------------------
if (isProducer)
{
    await KeyboardProducerLoop.RunAsync(
        session,
        cts.Token,
        eventType: 1);
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