using KeyboardSharingConsole.CommandLine;
using KeyboardSharingConsole.Consumers;
using KeyboardSharingConsole.Helpers;
using KeyboardSharingConsole.Models;
using KeyboardSharingConsole.Producers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;

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

logger.LogDebug("{Title}", Console.Title);

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

using var provider = await NamedPipeHelper.CreateNamedPipeConnectionProviderAsync(
    logger, options.LocalPeerName, options.RemotePeerName, cts.Token);

// ------------------------------------------------------------
// Layer 2: Protocol session (builder owns wiring)
// ------------------------------------------------------------
Console.WriteLine("creating protocol session");

// create and start the producer (events will be buffered until the session is started)
var keyboardNotificationQueue = new ConcurrentQueue<KeyPressedNotification>();
var keyboardNotificationEventConsumer = new KeyboardNotificationEventConsumer();
var keyboardNotificationRequestConsumer = new KeyboardNotificationRequestConsumer();

var sessionHost = SessionEndpointHelper.CreateSessionEndpoint(
    logger,
    isPeerA,
    provider,
    keyboardNotificationEventConsumer.OnEventReceived,
    keyboardNotificationRequestConsumer.OnRequestReceived);

// ------------------------------------------------------------
// Start request pump (pump internally calls StartAsync on the endpoint)
// ------------------------------------------------------------
Console.WriteLine("starting request pump");
var pumpTask =
    KeyboardNotificationRequestPump.RunRequestPumpAsync(
        keyboardNotificationQueue,
        sessionHost,
        cts.Token);

// ------------------------------------------------------------
// Start the keyboard notification generator
// ------------------------------------------------------------
var producer =
    new KeyboardNotificationProducer(keyboardNotificationQueue);

var producerTask = producer.RunAsync(cts.Token);

// ------------------------------------------------------------
// Wait for shutdown
// ------------------------------------------------------------
Console.WriteLine("Press Enter to exit...");
Console.ReadLine();
cts.Cancel();
try
{
    await Task.WhenAll(
        producerTask,
        pumpTask);
}
catch (OperationCanceledException)
{
    // Expected on shutdown
}
