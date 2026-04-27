using MWB.Networking.Layer1_Framing.Frames;

namespace MWB.Networking.Layer2_Protocol.Frames;

public enum ProtocolFrameKind : byte
{
    /*

    Event

    Request → Response* → Complete
                        → Error
                        → Cancel

    StreamOpen → StreamData* → StreamClose
                             → Error
                             → Cancel

    */

    /// <summary>
    /// A fire-and-forget frame that carries a discrete payload with no correlation
    /// to any request or stream. Events do not expect a response and are delivered
    /// exactly once to the receiver.
    ///
    /// Typical uses include user input (mouse, keyboard), notifications, or telemetry
    /// where ordering is preserved by the transport but application-level acknowledgment
    /// is not required.
    /// </summary>
    Event = NetworkFrameKind.Event,

    /// <summary>
    /// Initiates a correlated request identified by a RequestId. A request represents
    /// an explicit unit of intent and establishes a request lifecycle that may produce
    /// responses and/or streams.
    ///
    /// A request does not imply a one-to-one response; it may result in zero, one,
    /// or multiple response frames and streams.
    /// </summary>
    Request = 0x02,

    /// <summary>
    /// Carries a correlated response payload for a previously issued request.
    /// Multiple response frames may be sent for a single request, and responses
    /// may be interleaved with stream activity.
    ///
    /// A response frame does not indicate request completion.
    /// </summary>
    Response = 0x03,

    Error = 0x04,

    /// <summary>
    /// Announces the creation of a new logical stream identified by a StreamId.
    /// The payload contains opaque metadata describing the stream's purpose
    /// or characteristics.
    ///
    /// StreamOpen frames provide an explicit opportunity for the receiver to
    /// accept or refuse the stream before data transfer begins.
    /// </summary>
    StreamOpen = 0x10,

    /// <summary>
    /// Transfers a contiguous chunk of data belonging to an open stream.
    /// StreamData frames are ordered within a stream and may be interleaved
    /// with frames from other streams or requests.
    ///
    /// Chunk sequencing and completion are purely structural; higher layers
    /// determine how stream data is consumed or assembled.
    /// </summary>
    StreamData = 0x11,

    /// <summary>
    /// Indicates a normal, ordered termination of a stream.
    /// After this frame, no further StreamData frames will be sent for the StreamId.
    ///
    /// StreamClose affects only the individual stream and does not complete
    /// or cancel the associated request, if any.
    /// </summary>
    StreamClose = 0x12,

    /// <summary>
    /// Indicates an abnormal or premature termination of a stream due to error,
    /// refusal, or cancellation.
    ///
    /// A StreamAbort immediately ends the stream identified by the StreamId and
    /// invalidates any in-flight data for that stream. No further StreamData or
    /// StreamClose frames will be sent for the StreamId.
    ///
    /// Higher layers may interpret a StreamAbort as a failure or partial failure
    /// depending on the role of the stream within a request or operation; other
    /// streams and request lifecycles are otherwise unaffected.
    /// </summary>
    StreamAbort = 0x13
}
