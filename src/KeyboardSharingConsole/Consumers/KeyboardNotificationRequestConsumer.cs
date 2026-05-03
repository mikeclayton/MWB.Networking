using KeyboardSharingConsole.Models;
using MWB.Networking.Layer2_Protocol.Requests.Api;

namespace KeyboardSharingConsole.Consumers;

public sealed class KeyboardNotificationRequestConsumer
{
    private int _count;

    public int RequestCount => _count;

    public void OnRequestReceived(IncomingRequest request, ReadOnlyMemory<byte> payload)
    {
        try
        {
            var notification = KeyPressedNotification.FromPayload(payload);
            var acknowledgement = new KeyPressedAcknowledgement(notification.Key);

            Console.Write(notification.Key);

            // Respond explicitly
            request.Respond(acknowledgement.ToPayload());

            _count++;
        }
        catch (Exception ex)
        {
            request.Error();
        }
    }
}
