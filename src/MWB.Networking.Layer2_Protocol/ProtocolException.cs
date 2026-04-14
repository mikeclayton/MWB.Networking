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

    internal static ProtocolException InvalidFrameSequence(
        ProtocolFrame frame,
        string message)
    {
        return new ProtocolException(
            ProtocolErrorKind.InvalidFrameSequence,
            $"{message} (FrameKind={frame.Kind}, RequestId={frame.RequestId}, StreamId={frame.StreamId})");
    }

    internal static ProtocolException ProtocolViolation(
        ProtocolFrame frame,
        string message)
    {
        return new ProtocolException(
            ProtocolErrorKind.ProtocolViolation,
            $"{message} (FrameKind={frame.Kind}, RequestId={frame.RequestId}, StreamId={frame.StreamId})");
    }
}
