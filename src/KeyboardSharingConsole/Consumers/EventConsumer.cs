using KeyboardSharingConsole.Models;
using System.Collections.Concurrent;

namespace KeyboardSharingConsole.Consumers;

internal sealed class KeyboardNotificationEventConsumer
{
    public EventConsumer(ConcurrentQueue<KeyPressedNotification> inboundQueue)
    {
        this.InboundQueue = inboundQueue ?? throw new ArgumentNullException(nameof(InboundQueue));
    }

    private ConcurrentQueue<KeyPressedNotification> InboundQueue
    {
        get;
    }



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


}
