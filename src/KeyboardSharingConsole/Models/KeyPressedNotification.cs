using System.Text;

namespace KeyboardSharingConsole.Models;

internal class KeyPressedNotification
{
    public KeyPressedNotification(char key)
    {
        this.Key = key;
    }

    public char Key
    {
        get;
    }

    public ReadOnlyMemory<byte> ToPayload()
    {
        return Encoding.UTF8.GetBytes(this.Key.ToString());
    }

    public static KeyPressedNotification FromPayload(
        ReadOnlyMemory<byte> payload)
    {
        if (payload.IsEmpty)
            throw new ArgumentException(
                "KeyPressedNotification payload cannot be empty.",
                nameof(payload));

        var text = Encoding.UTF8.GetString(payload.Span);

        if (text.Length != 1)
            throw new ArgumentException(
                "KeyPressedNotification payload must contain exactly one character.",
                nameof(payload));

        return new KeyPressedNotification(text[0]);
    }
}
