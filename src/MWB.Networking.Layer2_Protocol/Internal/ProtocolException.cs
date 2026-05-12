namespace MWB.Networking.Layer2_Protocol.Internal;

public sealed class ProtocolException : Exception
{
    public ProtocolException(
        ProtocolErrorKind errorKind,
        string message)
        : base(message)
    {
        this.ErrorKind = errorKind;
    }

    public ProtocolException(
        ProtocolErrorKind errorKind,
        string message,
        Exception? innerException)
        : base(message, innerException)
    {
        this.ErrorKind = errorKind;
    }

    public ProtocolErrorKind ErrorKind
    {
        get;
    }

    internal static ProtocolException InvalidSequence(
        string message)
    {
        return new ProtocolException(
            ProtocolErrorKind.InvalidSequence,
            message);
    }

    internal static ProtocolException ProtocolViolation(
        string message)
    {
        return new ProtocolException(
            ProtocolErrorKind.ProtocolViolation,
            message);
    }
}
