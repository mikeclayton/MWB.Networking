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

    internal static ProtocolException DuplicateRequestId(
        string message)
    {
        return new ProtocolException(
            ProtocolErrorKind.DuplicateRequestId,
            message);
    }

    internal static ProtocolException DuplicateStreamId(
        string message)
    {
        return new ProtocolException(
            ProtocolErrorKind.DuplicateStreamId,
            message);
    }

    internal static ProtocolException StreamAborted(
        string message)
    {
        return new ProtocolException(
            ProtocolErrorKind.StreamAborted,
            message);
    }

    internal static ProtocolException InvalidSequence(
        string message)
    {
        return new ProtocolException(
            ProtocolErrorKind.InvalidSequence,
            message);
    }

    internal static ProtocolException InternalError(
        string message)
    {
        return new ProtocolException(
            ProtocolErrorKind.InternalError,
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
