using KeyboardSharingConsole.Models;
using MWB.Networking.Layer2_Protocol.Session.Api;
using System.Collections.Concurrent;

namespace KeyboardSharingConsole.Producers;

internal static class KeyboardNotificationEventPump
{
    public static async Task RunEventPumpAsync(
        ConcurrentQueue<KeyPressedNotification> queue,
        ProtocolSessionHandle session,
        CancellationToken ct,
        uint eventType = 1)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(session);

        // Pump owns protocol readiness
        await session.WhenReady.ConfigureAwait(false);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (queue.TryDequeue(out var notification))
                {
                    var payload = notification.ToPayload();

                    session.Commands.SendEvent(
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
