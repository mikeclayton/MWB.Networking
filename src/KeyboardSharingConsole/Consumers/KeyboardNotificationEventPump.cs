using KeyboardSharingConsole.Models;
using MWB.Networking.Layer3_Endpoint;
using System.Collections.Concurrent;

namespace KeyboardSharingConsole.Producers;

internal static class KeyboardNotificationEventPump
{
    public static async Task RunEventPumpAsync(
        ConcurrentQueue<KeyPressedNotification> queue,
        SessionEndpoint endpoint,
        CancellationToken ct,
        uint eventType = 1)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(endpoint);

        // Pump owns protocol readiness
        await endpoint.StartAsync(ct);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (queue.TryDequeue(out var notification))
                {
                    var payload = notification.ToPayload();

                    endpoint.SendEvent(
                        eventType: eventType,
                        payload: payload);
                }
                else
                {
                    // Prevent hot spinning
                    await Task.Yield();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown path
        }
        catch (IOException)
        {
            // other peer closed the pipe
        }
    }
}
