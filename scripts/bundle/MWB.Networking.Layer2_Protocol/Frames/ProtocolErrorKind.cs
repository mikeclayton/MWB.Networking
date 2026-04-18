namespace MWB.Networking.Layer2_Protocol.Frames;

public enum ProtocolErrorKind
{
    // Structural / routing errors
    UnknownFrameKind,
    UnknownRequestId,
    UnknownStreamId,

    // Lifecycle violations
    DuplicateRequestId,
    DuplicateStreamId,
    InvalidFrameSequence,
    DuplicateTerminalFrame,

    // Missing or malformed fields
    MissingRequestId,
    MissingStreamId,

    // General / fallback
    ProtocolViolation
}
