using KeyboardSharingConsole.Models;
using System.Collections.Concurrent;

namespace KeyboardSharingConsole.Producers;

internal sealed class KeyboardNotificationProducer
{
    public KeyboardNotificationProducer(ConcurrentQueue<KeyPressedNotification> outboundQueue)
    {
        this.OutboundQueue = outboundQueue ?? throw new ArgumentNullException(nameof(outboundQueue));
    }

    private ConcurrentQueue<KeyPressedNotification> OutboundQueue
    {
        get;
    }

    public async Task<int> RunAsync(
        CancellationToken cancellationToken)
    {
        Console.WriteLine("[KEYBOARD] Type keys to send. Press ESC to quit.");

        var sentCount = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var keyInfo = Console.ReadKey(intercept: true);

            // Encode the key as UTF‑8
            var notification = new KeyPressedNotification(keyInfo.KeyChar);
            var payload = notification.ToPayload();

            //// send payload as an event
            //session.Commands.SendEvent(
            //    eventType: eventType,
            //    payload: payload);

            //// send payload as a request and await response
            //var request = session.Commands.SendRequest(
            //    requestType: 1,
            //    payload: payload);
            //var responseFrame = await request.Response
            //    .WaitAsync(cancellationToken);

            //// validate the response
            //var acknowledgement =
            //    KeyPressedAcknowledgement.FromPayload(responseFrame.Payload);

            //// Optional: validate response
            //if (acknowledgement.Key != notification.Key)
            //{
            //    Console.WriteLine(
            //        $"[WARN] Remote echoed '{acknowledgement.Key}', expected '{notification.Key}'");
            //}

            //sentCount++;

            this.OutboundQueue.Enqueue(notification);

            // Optional local echo
            Console.Write(keyInfo.KeyChar);

            if (keyInfo.Key == ConsoleKey.Escape)
            {
                Console.WriteLine();
                Console.WriteLine("[KEYBOARD] ESC sent, exiting.");
                break;
            }
        }

        return sentCount;
    }
}
