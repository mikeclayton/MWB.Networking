namespace MWB.Networking.Layer2_Protocol;

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
}
