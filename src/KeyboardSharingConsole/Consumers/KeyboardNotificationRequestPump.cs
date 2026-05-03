using KeyboardSharingConsole.Models;
using MWB.Networking.Layer3_Endpoint;
using System.Collections.Concurrent;

namespace KeyboardSharingConsole.Producers;

internal static class KeyboardNotificationRequestPump
{
    public static async Task RunRequestPumpAsync(
        ConcurrentQueue<KeyPressedNotification> queue,
        SessionEndpoint endpoint,
        CancellationToken ct,
        uint requestType = 1)
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

                    var request = endpoint.SendRequest(
                        requestType: requestType,
                        payload: payload);

                    var responseFrame = await request.Response
                        .WaitAsync(ct)
                        .ConfigureAwait(false);

                    var acknowledgement = KeyPressedAcknowledgement.FromPayload(
                            responseFrame.Payload);

                    if (acknowledgement.Key != notification.Key)
                    {
                        Console.WriteLine(
                            $"[WARN] Remote echoed '{acknowledgement.Key}', expected '{notification.Key}'");
                    }
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
