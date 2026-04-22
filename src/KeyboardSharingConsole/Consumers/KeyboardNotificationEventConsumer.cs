using System.Text;

namespace KeyboardSharingConsole.Consumers;

public sealed class KeyboardNotificationEventConsumer
{
    private int _count;

    public int EventCount => _count;

    public void OnEventReceived(uint eventType, ReadOnlyMemory<byte> payload)
    {
        var text = Encoding.UTF8.GetString(payload.Span);
        Interlocked.Increment(ref _count);

        // ESC sentinel
        if (text.Length > 0 && text[0] == (char)27)
        {
            Console.WriteLine();
            Console.WriteLine("[CONSUMER] ESC received. Remote exit signaled.");
            return;
        }

        Console.Write(text);
    }
}
