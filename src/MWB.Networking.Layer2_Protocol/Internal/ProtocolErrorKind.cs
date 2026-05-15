namespace MWB.Networking.Layer2_Protocol.Internal;

public enum ProtocolErrorKind
{
    // Structural / routing errors
    UnknownFrameKind,
    UnknownRequestId,
    UnknownStreamId,

    // Lifecycle violations
    DuplicateRequestId,
    DuplicateStreamId,
    InvalidSequence,
    DuplicateTerminalFrame,
    StreamAborted,

    // Missing or malformed fields
    MissingRequestId,
    MissingStreamId,

    // General
    ProtocolViolation,

    // Fallback
    InternalError
}
