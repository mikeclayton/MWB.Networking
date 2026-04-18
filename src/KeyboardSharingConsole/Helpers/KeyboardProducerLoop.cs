using MWB.Networking.Layer2_Protocol.Session.Api;
using System.Text;

namespace KeyboardSharingConsole.Helpers
{
    public static class KeyboardProducerLoop
    {
        public static async Task<int> RunAsync(
            ProtocolSessionHandle session,
            CancellationToken cancellationToken,
            uint eventType = 1)
        {
            ArgumentNullException.ThrowIfNull(session);

            await session.WhenReady.ConfigureAwait(false);

            Console.WriteLine("[KEYBOARD] Type keys to send. Press ESC to quit.");

            var sentCount = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                var keyInfo = Console.ReadKey(intercept: true);

                // Encode the key as UTF‑8
                var payload =
                    Encoding.UTF8.GetBytes(keyInfo.KeyChar.ToString());

                session.Commands.SendEvent(
                    eventType: eventType,
                    payload: payload);

                sentCount++;

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
}
